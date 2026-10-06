using MarionGI.Api;
using MarionGI.Api.Middlewares;
using MarionGI.Api.PaiementProvider;
using MarionGI.Application.Interfaces;
using MarionGI.Application.Services;
using MarionGI.Infrastructure.Identity;
using MarionGI.Infrastructure.Pdf;
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

// 6. Autorisations & Policies (Correction de l'erreur 500)
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Administrateur"));

    options.AddPolicy("GestionnaireOrAdmin", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire"));

    // 🎯 Lecture
    options.AddPolicy("Contrats.Read", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire", "Locataire"));

    // 🎯 Création
    options.AddPolicy("Contrats.Create", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire"));

    // 🎯 Modification (résout le crash HTTP 500)
    options.AddPolicy("Contrats.Update", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire"));

    // 🎯 Suppression
    options.AddPolicy("Contrats.Delete", policy =>
        policy.RequireRole("Administrateur"));

    // 🎯 Politiques pour les Biens Immobiliers
    options.AddPolicy("Biens.Read", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire", "Agent", "Locataire"));

    options.AddPolicy("Biens.Create", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire"));

    options.AddPolicy("Biens.Update", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire"));

    options.AddPolicy("Biens.Delete", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire"));

    // 🎯 Politiques pour les Demandes de Visite
    options.AddPolicy("DemandesVisite.Read", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire", "Agent"));

    options.AddPolicy("DemandesVisite.Create", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire", "Agent"));

    options.AddPolicy("DemandesVisite.Update", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire", "Agent"));

    options.AddPolicy("DemandesVisite.Delete", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire"));
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