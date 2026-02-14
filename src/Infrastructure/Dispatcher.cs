using Application.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public sealed class Dispatcher(IServiceProvider provider) : IDispatcher
{
    public async Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
    {
        var type = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult));
        var handler = provider.GetRequiredService(type);
        return await ((dynamic)handler).HandleAsync((dynamic)command, cancellationToken);
    }

    public async Task<TResult> QueryAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
    {
        var type = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResult));
        var handler = provider.GetRequiredService(type);
        return await ((dynamic)handler).HandleAsync((dynamic)query, cancellationToken);
    }
}
