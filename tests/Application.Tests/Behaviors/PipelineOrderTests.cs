using Application.Abstractions.Common;
using Application.Behaviors;
using Application.Projects.Commands.CreateProject;
using Application.Tests.Fakes;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Application.Tests.Behaviors;

// El orden de los behaviors del pipeline no es un detalle de estilo: define
// que se responde antes, que queda registrado y si el payload de una peticion
// llega a tocarse.
//
// MediatR ejecuta los behaviors en el ORDEN DE REGISTRO: el primero registrado
// es el mas externo y se ejecuta primero. Por dentro usa
// GetServices(...).Reverse() y despues envuelve cada behavior alrededor del
// anterior, asi que los dos reveses se cancelan. Por eso el orden registrado
// tiene que ser:
//
//   Audit -> Logging -> Authorization -> Validation -> Handler
//
// Los dos observers (Audit y Logging) van afuera para ver el veredicto
// completo, incluidas las peticiones que se rechazan. Las dos puertas
// (Authorization y Validation) van adentro: primero se verifica quien llama y
// recien despues se toca el payload, asi un cliente anonimo no puede descubrir
// el esquema de un endpoint admin.
//
// Este test falla si alguien cambia el orden de los registros, que es
// exactamente como la documentacion se desincroniza sin avisar.
public class PipelineOrderTests
{
    [Fact]
    public void Behaviors_AreRegisteredInExecutionOrder()
    {
        var services = new ServiceCollection();

        // MediatR exige un ILoggerFactory registrado antes de AddMediatR.
        services.AddLogging();
        services.AddSingleton<ICurrentUser, FakeCurrentUser>();
        services.AddApplication();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        // El orden en que el contenedor resuelve los behaviors es el orden de
        // registro, que es el mismo que MediatR usa para ejecutarlos.
        var behaviors = scope.ServiceProvider
            .GetServices<IPipelineBehavior<CreateProjectCommand, Result<Guid>>>()
            .Select(behavior => behavior.GetType().GetGenericTypeDefinition())
            .ToList();

        behaviors.Should().Equal(
            typeof(AuditBehavior<,>),
            typeof(LoggingBehavior<,>),
            typeof(AuthorizationBehavior<,>),
            typeof(ValidationBehavior<,>));
    }
}
