using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Application.Messaging;

internal sealed class Sender : ISender
{
    private static readonly ConcurrentDictionary<Type, RequestHandlerWrapperBase> Wrappers = new();

    private readonly IServiceProvider _serviceProvider;

    public Sender(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        RequestHandlerWrapperBase wrapper = Wrappers.GetOrAdd(request.GetType(), requestType =>
        {
            Type wrapperType = typeof(RequestHandlerWrapper<,>).MakeGenericType(requestType, typeof(TResponse));
            return (RequestHandlerWrapperBase)Activator.CreateInstance(wrapperType)!;
        });

        return ((RequestHandlerWrapper<TResponse>)wrapper).Handle(request, _serviceProvider, cancellationToken);
    }

    private abstract class RequestHandlerWrapperBase;

    private abstract class RequestHandlerWrapper<TResponse> : RequestHandlerWrapperBase
    {
        public abstract Task<TResponse> Handle(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
    }

    private sealed class RequestHandlerWrapper<TRequest, TResponse> : RequestHandlerWrapper<TResponse>
        where TRequest : IRequest<TResponse>
    {
        public override Task<TResponse> Handle(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            var handler = serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();

            RequestHandlerDelegate<TResponse> pipeline = () => handler.Handle((TRequest)request, cancellationToken);

            IEnumerable<IPipelineBehavior<TRequest, TResponse>> behaviors =
                serviceProvider.GetServices<IPipelineBehavior<TRequest, TResponse>>().Reverse();

            foreach (var behavior in behaviors)
            {
                RequestHandlerDelegate<TResponse> next = pipeline;
                pipeline = () => behavior.Handle((TRequest)request, next, cancellationToken);
            }

            return pipeline();
        }
    }
}
