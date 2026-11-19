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
        
        // MediatR ejecuta los behaviors en el ORDEN DE REGISTRO: el primero
        // registrado es el más externo y se ejecuta primero. Por dentro usa
        // GetServices(...).Reverse() y después envuelve cada behavior alrededor
        // del anterior, así que los dos reveses se cancelan.
        //
        // Orden resultante: Audit → Logging → Authorization → Validation → Handler.
        //
        // Los dos observers (Audit y Logging) van afuera para ver el resultado
        // completo, incluidas las peticiones que se rechazan. Las dos puertas
        // (Authorization y Validation) van adentro: primero se verifica quién
        // llama y recién después se toca el payload, así un cliente anónimo no
        // puede descubrir el esquema de un endpoint admin. Es el orden del
        // roadmap (docs/roadmap-implementacion.md), con Audit todavía más
        // afuera para que la auditoría también registre los intentos rechazados.
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AuditBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        
        return services;
    }
}
