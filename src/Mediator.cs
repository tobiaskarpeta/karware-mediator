using System.Collections.Concurrent;
using System.Reflection;
using Karware.Mediator.Requests;

namespace Karware.Mediator;

public sealed class Mediator : IMediator
{
    private const string HandlerMethodName = nameof(IRequestHandler<>.HandleAsync);

    private static readonly ConcurrentDictionary<Type, MethodInfo> MethodCache = new();

    private readonly IServiceProvider _serviceProvider;

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task Send(IRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();
        var handlerType = typeof(IRequestHandler<>).MakeGenericType(requestType);

        var (handler, handleMethod) = ResolveHandlerAndMethod(requestType, handlerType);

        var resultTask = handleMethod.Invoke(handler, [request, cancellationToken]);
        if (resultTask is not Task task)
        {
            throw new InvalidOperationException(
                $"Handler '{handlerType.FullName}' returned '{resultTask?.GetType().FullName ?? "null"}' " +
                $"instead of a Task while handling request '{requestType.FullName}'.");
        }

        await task.ConfigureAwait(false);
    }

    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));

        var (handler, handleMethod) = ResolveHandlerAndMethod(requestType, handlerType);

        var resultTask = handleMethod.Invoke(handler, [request, cancellationToken]);
        if (resultTask is not Task<TResponse> typedTask)
        {
            throw new InvalidOperationException(
                $"Handler '{handlerType.FullName}' returned '{resultTask?.GetType().FullName ?? "null"}' " +
                $"instead of a Task<{typeof(TResponse).FullName}> while handling request '{requestType.FullName}'.");
        }

        return await typedTask.ConfigureAwait(false);
    }

    private (object handler, MethodInfo handleMethod) ResolveHandlerAndMethod(Type requestType, Type handlerType)
    {
        var handler = _serviceProvider.GetService(handlerType)
            ?? throw new InvalidOperationException($"No handler registered for request type '{requestType.FullName}'");

        var handleMethod = MethodCache.GetOrAdd(handlerType, static type =>
        {
            var method = type.GetMethod(HandlerMethodName, BindingFlags.Public | BindingFlags.Instance);
            return method ?? throw new InvalidOperationException(
                $"Handler type '{type.FullName}' does not define a public instance method named '{HandlerMethodName}'.");
        });

        return (handler, handleMethod);
    }
}