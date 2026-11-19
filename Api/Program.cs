using System.Text;
using Api.Common;
using Api.Endpoints;
using Infrastructure.Authentication;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
    ?? throw new InvalidOperationException("Jwt section not found in configuration");

const string JwtScheme = "Bearer";

builder.Services.AddAuthentication(options =>
    {
        // Force every default to the JWT scheme. AddIdentity runs before us and
        // configures the cookie scheme as the challenge/forbid scheme, which is
        // why RequireAuthorization failures were redirecting to /Account/Login
        // even though the JWT itself validated OK.
        options.DefaultScheme = JwtScheme;
        options.DefaultAuthenticateScheme = JwtScheme;
        options.DefaultChallengeScheme = JwtScheme;
        options.DefaultForbidScheme = JwtScheme;
    })
    .AddJwtBearer(JwtScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
            // RoleClaimType and NameClaimType intentionally left at their defaults
            // (ClaimTypes.Role and ClaimTypes.Name) because JwtSecurityTokenHandler
            // maps the "role" and "sub" JWT claims to those standard ClaimTypes when
            // DefaultMapInboundClaims is true. Overriding these to "role"/"sub"
            // would make RequireRole look under a type the mapper already rewrote.
        };

        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // Read token from cookie if Authorization header is not present
                if (string.IsNullOrEmpty(context.Token))
                {
                    context.Token = context.Request.Cookies[CookieHelpers.AuthCookieName];
                }
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                // Belt-and-suspenders: even though JwtBearer never redirects by
                // default, force a clean 401 (no Location header, no ProblemDetails)
                // so a frontend probe can never bounce to /Account/Login again.
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddProblemDetails();
// El handler de dominio se consulta antes que el ProblemDetailsService:
// una violacion de las reglas de negocio es un 400, no un 500.
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();

var allowedOrigins = builder.Configuration["Cors:AllowedOrigins"]?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowCredentials()
                  .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                  .WithHeaders("Content-Type", "Authorization");
        }
        else
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

builder.Services.AddAuthorization(opts =>
    opts.AddPolicy("AdminOnly", p => p.RequireRole("Admin")));

var app = builder.Build();

//Migracion automatica de la base de datos.
//Se activa con Database:AutoMigrate (por defecto false para no cambiar el comportamiento local).
//Es lo que permite que un contenedor arrancque con una base SQLite vacia y quede usable.
if (app.Configuration.GetValue<bool>("Database:AutoMigrate"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    app.Logger.LogInformation("Aplicando migraciones de base de datos...");
    await db.Database.MigrateAsync();
    app.Logger.LogInformation("Migraciones de base de datos aplicadas correctamente.");
}

app.UseExceptionHandler("");
app.UseStatusCodePages();

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapPublicProjectEndpoints();
app.MapPublicContributionEndpoints();
app.MapAdminProjectEndpoints();
app.MapAdminContributionEndpoints();

//Health check anonimo: lo usan el HEALTHCHECK del contenedor y el smoke test de CI.
//Devuelve 200 si la base de datos responde y 503 si no.
app.MapGet("/health", async (AppDbContext db, CancellationToken cancellationToken) =>
{
    try
    {
        var canConnect = await db.Database.CanConnectAsync(cancellationToken);

        return canConnect
            ? Results.Ok(new { status = "healthy", database = "connected" })
            : Results.Json(
                new { status = "unhealthy", database = "unreachable" },
                statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Fallo en el health check: no se pudo conectar con la base de datos");

        return Results.Json(
            new { status = "unhealthy", database = "unreachable" },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
})
.WithTags("Health")
.WithName("Health");

app.Run();
