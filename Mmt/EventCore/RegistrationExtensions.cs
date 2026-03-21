using Microsoft.Extensions.DependencyInjection;

namespace EventCore;

internal static class ServiceProviderExtensions
{
    extension(IServiceProvider serviceProvider)
    {
        public IStartsWithProxy<TEntity> GetProxy<TEntity>(ICreateEvent @event)
        {
            var key = (@event.GetType(), typeof(TEntity));
            return serviceProvider.GetRequiredKeyedService<IStartsWithProxy<TEntity>>(key);
        }

        public IEnumerable<IEntity> CreateEntities(ICreateEvent @event)
        {
            var key = @event.GetType();
            var creators = serviceProvider.GetKeyedServices<IStartsWithProxy>(key);
            foreach (var handles in creators)
            {
                yield return handles.Create(@event);
            }
        }

        public IHandlesProxy<TEntity> GetProxy<TEntity>(IEvent @event)
        {
            var key = (@event.GetType(), typeof(TEntity));
            return serviceProvider.GetRequiredKeyedService<IHandlesProxy<TEntity>>(key);
        }

        public IEntity Handle(IEvent @event, IEntity entity)
        {
            var key = (@event.GetType(), entity.GetType());
            var handlesProxy = serviceProvider.GetRequiredKeyedService<IHandlesProxy>(key);

            return handlesProxy.Handle(@event, entity);
        }

        public async Task BroadcastEventAsync(IEvent @event, IEntity entity)
        {
            var key = (@event.GetType(), entity.GetType());
            var handlers = serviceProvider.GetKeyedServices<IEventListenerProxy>(key);
            foreach (var handler in handlers)
            {
                await handler.HandleAsync(@event, entity);
            }
        }
    }
}
