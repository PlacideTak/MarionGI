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

builder.Services.Configure<NotchpayPaiementOptions>(builder.Configuration.GetSection("PaymentProvider"));
builder.Services.AddHttpClient<IPaiementProvider, NotchpayPaiementProvider>();

// 4. Extraction et validation de la clé JWT
// Extraire la clé secrète en supprimant d'éventuels espaces
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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!.Trim())),

        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"]?.Trim(),

        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"]?.Trim(),

        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(5),

        // Utiliser les ClaimTypes natifs de .NET
        RoleClaimType = ClaimTypes.Role,
        NameClaimType = ClaimTypes.Email
    };
});

// 6. Autorisations & Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Administrateur"));
    options.AddPolicy("GestionnaireOrAdmin", policy => policy.RequireRole("Administrateur", "Gestionnaire"));
});

// 7. Contrôleurs & CORS
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Ignore les boucles infinies de navigation
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularApp", p =>
    {
        p.SetIsOriginAllowed(origin => true) // Autorise Angular et Swagger en local
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
        Type = SecuritySchemeType.Http, // 👈 Changé en ApiKey pour éviter tout formatage automatique
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Entrez uniquement votre token JWT (Exemple: Bearer eyJhbGci...)"
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

// Intercepteur global des exceptions
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

// L'ordre ci-dessous est critique : CORS -> Authentication -> Authorization
app.UseCors("AngularApp");

app.UseAuthentication();
app.UseAuthorization();
app.UseExceptionHandler();
app.MapControllers();

app.Run();