using Application.Abstractions.Authentication;
using Application.Abstractions.Common;
using Application.Abstractions.Persistence;
using Domain.Entities;
using Infrastructure.Authentication;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Configuration;                                                                                                                                                                                            
using Microsoft.Extensions.DependencyInjection;                                                                                                                                                                                      
using Microsoft.EntityFrameworkCore;                                                                                                                                                                                                 
using Microsoft.AspNetCore.Identity; 

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
// Base de datos.
        // El path se resuelve de forma estable (anchored al repo + overrides via env var)
        // para que el runtime y `dotnet ef` apunten al MISMO archivo sin depender del cwd.
        // Ver Infrastructure/Persistence/DatabasePath.cs para el rationale.
        var connectionString = DatabaseConnection.Resolve();
        //Registramos el contexto de Entity Framework. Por defecto, AppDbContext lo registra como scoped
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString));
        
        //Identity (autenticacion y usuarios locales)
        //Registramos Identity y le pasamos reglas de validacion para las contraseñas
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
            })
            .AddEntityFrameworkStores<AppDbContext>();

        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IContributionRepository, ContributionRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.Configure<BootstrapSettings>(configuration.GetSection("Bootstrap"));

        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IBootstrapTokenValidator, BootstrapTokenValidator>();

        services.AddScoped<ICurrentUser, CurrentUser>();
        
        services.AddHttpContextAccessor();
        
        return services;
    }
}
