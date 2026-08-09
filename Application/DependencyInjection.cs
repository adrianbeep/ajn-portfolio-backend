using FluentValidation;
using Application.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(config =>
            config.RegisterServicesFromAssembly(assembly));
        
        services.AddValidatorsFromAssembly(assembly);
        
        //El orden de registro importa, es el orden en el que se van a ejecutar de
        //"afuera hacia adentro"
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AuditBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        
        return services;
    }
}
