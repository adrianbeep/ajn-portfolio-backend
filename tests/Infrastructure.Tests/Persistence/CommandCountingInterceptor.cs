using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Infrastructure.Tests.Persistence;

// Interceptor que registra los comandos que se ejecutan contra la base. Sirve
// para demostrar que el listado de proyectos mantiene su numero de viajes
// (round-trips) despues de agregar la columna de tecnologias.
//
// Solo se registran los comandos: el SQL de cada uno queda guardado para
// poder distinguir un COUNT de un SELECT.
public sealed class CommandCountingInterceptor : DbCommandInterceptor
{
    private readonly List<string> _commands = new();

    // Copia defensiva: la lista se lee desde el hilo de la prueba mientras el
    // contexto puede seguir escribiendo.
    public IReadOnlyList<string> Commands
    {
        get
        {
            lock (_commands)
            {
                return _commands.ToArray();
            }
        }
    }

    public int Count => Commands.Count;

    // Se llama justo antes de la operacion que se quiere medir, para que el
    // montage no contamne el conteo.
    public void Reset()
    {
        lock (_commands)
        {
            _commands.Clear();
        }
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        Record(command);
        return base.NonQueryExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Record(command);
        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        Record(command);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        Record(command);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result)
    {
        Record(command);
        return base.ScalarExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        Record(command);
        return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }

    private void Record(DbCommand command)
    {
        lock (_commands)
        {
            _commands.Add(command.CommandText);
        }
    }
}
