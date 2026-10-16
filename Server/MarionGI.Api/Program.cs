using MarionGI.Api;
using MarionGI.Api.Middlewares;
using MarionGI.Api.PaiementProvider;
using MarionGI.Application.Interfaces;
using MarionGI.Application.Services;
using MarionGI.Infrastructure.Identity;
using MarionGI.Infrastructure.Pdf;
using MarionGI.Infrastructure.Services;
using MarionGI.Infrastructure.Sms;
using MarionGI.Persistence.Context;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// 2. Configuration de Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/marion_log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// 3. Base de données & Services
builder.Services.AddDbContext<MarionDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails(); // requis par AddExceptionHandler

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISmsService, MockSmsService>();
builder.Services.AddScoped<IQuittancePdfService, QuittancePdfService>();
builder.Services.AddScoped<IRapportPdfService, RapportPdfService>();

// 🎯 AJOUT: Service métier pour les Contrats
//builder.Services.AddScoped<IContratService, ContratService>();

builder.Services.Configure<NotchpayPaiementOptions>(builder.Configuration.GetSection("PaymentProvider"));
builder.Services.AddHttpClient<IPaiementProvider, NotchpayPaiementProvider>();

// 4. Extraction et validation de la clé JWT
var secretKey = builder.Configuration["Jwt:SecretKey"]?.Trim();
if (string.IsNullOrEmpty(secretKey))
{
    throw new InvalidOperationException("La clé secrète JWT ('Jwt:SecretKey') n'est pas configurée.");
}

var symmetricKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = symmetricKey,

        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"]?.Trim(),

        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"]?.Trim(),

        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,

        RoleClaimType = ClaimTypes.Role,
        NameClaimType = ClaimTypes.Email
    };
});

// ============================================================
// 6. AUTORISATIONS & POLICIES
// ============================================================

builder.Services.AddAuthorization(options =>
{
    // ========================================================
    // POLICIES GÉNÉRALES
    // ========================================================

    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Administrateur"));

    options.AddPolicy("GestionnaireOrAdmin", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire"));

    options.AddPolicy("PersonnelGestion", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire", "Agent"));


    // ========================================================
    // VALEURS FISCALES DES BIENS
    // ========================================================

    // Lecture : Administrateur et Gestionnaire
    options.AddPolicy("ValeursFiscalesBiens.Read", policy =>
        policy.RequireRole(
            "Administrateur",
            "Gestionnaire"));

    // Création : Administrateur et Gestionnaire
    options.AddPolicy("ValeursFiscalesBiens.Create", policy =>
        policy.RequireRole(
            "Administrateur",
            "Gestionnaire"));

    // Modification : Administrateur et Gestionnaire
    options.AddPolicy("ValeursFiscalesBiens.Update", policy =>
        policy.RequireRole(
            "Administrateur",
            "Gestionnaire"));

    // Suppression logique : Administrateur et Gestionnaire
    options.AddPolicy("ValeursFiscalesBiens.Delete", policy =>
        policy.RequireRole(
            "Administrateur",
            "Gestionnaire"));


    // ========================================================
    // UTILISATEURS
    // ========================================================

    options.AddPolicy("Utilisateurs.Read", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire"));

    options.AddPolicy("Utilisateurs.Create", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire"));

    options.AddPolicy("Utilisateurs.Update", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire"));

    options.AddPolicy("Utilisateurs.UpdateStatut", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire"));

    options.AddPolicy("Utilisateurs.Delete", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire"));

    // ========================================================
    // SOCIÉTÉS
    // ========================================================

    options.AddPolicy("Societes.Read", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire"));

    options.AddPolicy("Societes.Create", policy =>
        policy.RequireRole("Administrateur"));

    options.AddPolicy("Societes.Update", policy =>
        policy.RequireRole("Administrateur"));

    options.AddPolicy("Societes.Delete", policy =>
        policy.RequireRole("Administrateur"));

    // ========================================================
    // CONTRATS
    // ========================================================

    options.AddPolicy("Contrats.Read", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire", "Locataire"));

    options.AddPolicy("Contrats.Create", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire"));

    options.AddPolicy("Contrats.Update", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire"));

    options.AddPolicy("Contrats.Delete", policy =>
        policy.RequireRole("Administrateur"));

    // ========================================================
    // BIENS IMMOBILIERS
    // ========================================================

    options.AddPolicy("Biens.Read", policy =>
        policy.RequireRole(
            "Administrateur", "Gestionnaire", "Agent", "Locataire"));

    options.AddPolicy("Biens.Create", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire"));

    options.AddPolicy("Biens.Update", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire"));

    options.AddPolicy("Biens.Delete", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire"));

    // ========================================================
    // UNITÉS LOCATIVES
    // ========================================================

    options.AddPolicy("UnitesLocatives.Read", policy =>
        policy.RequireRole(
            "Administrateur", "Gestionnaire", "Agent", "Locataire"));

    options.AddPolicy("UnitesLocatives.Create", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire"));

    options.AddPolicy("UnitesLocatives.Update", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire"));

    options.AddPolicy("UnitesLocatives.Delete", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire"));

    // ========================================================
    // PAIEMENTS
    // ========================================================

    // Le locataire ne doit accéder qu'à ses propres paiements
    // et aux contrats dont il est titulaire.
    options.AddPolicy("Paiements.Read", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire", "Locataire"));

    options.AddPolicy("Paiements.Create", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire", "Locataire"));

    options.AddPolicy("Paiements.Update", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire"));

    options.AddPolicy("Paiements.Delete", policy =>
        policy.RequireRole("Administrateur"));

    // Initialisation d'un paiement en ligne.
    options.AddPolicy("Paiements.Initier", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire", "Locataire"));

    // Le webhook du fournisseur de paiement doit être sécurisé
    // par vérification de signature, pas par un rôle utilisateur.

    // ========================================================
    // CHARGES IMMOBILIÈRES
    // ========================================================

    options.AddPolicy("Charges.Read", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire"));

    options.AddPolicy("Charges.Create", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire"));

    options.AddPolicy("Charges.Update", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire"));

    options.AddPolicy("Charges.Delete", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire"));

    // ========================================================
    // PRESTATAIRES
    // ========================================================

    options.AddPolicy("Prestataires.Read", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire"));

    options.AddPolicy("Prestataires.Create", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire"));

    options.AddPolicy("Prestataires.Update", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire"));

    options.AddPolicy("Prestataires.Delete", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire"));

    // ========================================================
    // TICKETS D'INCIDENT
    // ========================================================

    // La visibilité des tickets doit être limitée dans le contrôleur :
    // locataire : ses tickets ;
    // personnel : tickets de sa société.
    options.AddPolicy("Tickets.Read", policy =>
        policy.RequireRole(
            "Administrateur", "Gestionnaire", "Agent", "Locataire"));

    options.AddPolicy("Tickets.Create", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire", "Locataire"));

    options.AddPolicy("Tickets.Update", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire"));

    options.AddPolicy("Tickets.Delete", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire"));

    options.AddPolicy("Tickets.AssignerIntervenant", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire"));

    options.AddPolicy("Tickets.ValiderDevis", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire"));

    options.AddPolicy("Tickets.Cloturer", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire"));

    // ========================================================
    // DEMANDES DE VISITE
    // ========================================================

    options.AddPolicy("DemandesVisite.Read", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire", "Agent"));

    options.AddPolicy("DemandesVisite.Create", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire", "Agent"));

    options.AddPolicy("DemandesVisite.Update", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire", "Agent"));

    options.AddPolicy("DemandesVisite.Delete", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire"));

    // ========================================================
    // TRANCHES IRPP
    // ========================================================

    options.AddPolicy("TranchesIRPP.Read", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire"));

    options.AddPolicy("TranchesIRPP.Create", policy =>
        policy.RequireRole("Administrateur"));

    options.AddPolicy("TranchesIRPP.Update", policy =>
        policy.RequireRole("Administrateur"));

    options.AddPolicy("TranchesIRPP.Delete", policy =>
        policy.RequireRole("Administrateur"));

    // ========================================================
    // RÉGIMES FISCAUX
    // ========================================================

    options.AddPolicy("RegimesFiscaux.Read", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire"));

    options.AddPolicy("RegimesFiscaux.Create", policy =>
        policy.RequireRole("Administrateur"));

    options.AddPolicy("RegimesFiscaux.Update", policy =>
        policy.RequireRole("Administrateur"));

    options.AddPolicy("RegimesFiscaux.Delete", policy =>
        policy.RequireRole("Administrateur"));

    // ========================================================
    // PARAMÈTRES FISCAUX
    // ========================================================

    options.AddPolicy("ParametresFiscaux.Read", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire"));

    options.AddPolicy("ParametresFiscaux.Create", policy =>
        policy.RequireRole("Administrateur"));

    options.AddPolicy("ParametresFiscaux.Update", policy =>
        policy.RequireRole("Administrateur"));

    options.AddPolicy("ParametresFiscaux.Delete", policy =>
        policy.RequireRole("Administrateur"));

    // ========================================================
    // RAPPORTS
    // ========================================================

    options.AddPolicy("Rapports.Read", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire"));

    options.AddPolicy("Rapports.Export", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire"));

    // ========================================================
    // NOTIFICATIONS
    // ========================================================

    options.AddPolicy("Notifications.Read", policy =>
        policy.RequireRole(
            "Administrateur", "Gestionnaire", "Agent", "Locataire"));

    options.AddPolicy("Notifications.Update", policy =>
        policy.RequireRole(
            "Administrateur", "Gestionnaire", "Agent", "Locataire"));

    options.AddPolicy("Notifications.Delete", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire"));

    // ========================================================
    // JOURNAL D'AUDIT
    // ========================================================

    options.AddPolicy("JournalAudit.Read", policy =>
        policy.RequireRole("Administrateur"));

    options.AddPolicy("JournalAudit.Export", policy =>
        policy.RequireRole("Administrateur"));
});


// 7. Contrôleurs & CORS
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularApp", p =>
    {
        p.SetIsOriginAllowed(origin => true)
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials();
    });
});

// 8. Documentation Swagger UI
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "MarionGI API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Entrez votre token JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ============================================================================
// PIPELINE MIDDLEWARE
// ============================================================================

var app = builder.Build();

app.UseStaticFiles();

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MarionGI API v1");
    });
}

app.UseHttpsRedirection();

// Ordre Middleware : CORS -> Authentication -> Authorization
app.UseCors("AngularApp");

app.UseAuthentication();
app.UseAuthorization();
app.UseExceptionHandler();
app.MapControllers();

app.Run();