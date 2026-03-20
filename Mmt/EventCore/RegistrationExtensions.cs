using Microsoft.Extensions.DependencyInjection;

namespace EventCore;

// TODO: cache these proxies
internal static class RegistrationExtensions
{
    extension(StartsWithEventRegistration[] registrations)
    {
        public IStartsWithProxy<TEntity> GetProxy<TEntity>()
        {
            var startsWith = registrations.Single(x => x.EntityType == typeof(TEntity));
            var proxyType = typeof(StartsWithProxy<,>).MakeGenericType(startsWith.EventType, typeof(TEntity));
            return Activator.CreateInstance(proxyType) as IStartsWithProxy<TEntity> ?? throw new InvalidOperationException("Failed to create proxy");
        }

        public IEnumerable<IEntity> CreateEntities(ICreateEvent createEvent)
        {
            var creators = registrations.Where(x => x.EventType == createEvent.GetType());
            foreach (var handles in creators)
            {
                var proxyType = typeof(StartsWithProxy<,>).MakeGenericType(handles.EventType, handles.EntityType);
                if (Activator.CreateInstance(proxyType) is IStartsWithProxy startsWithProxy)
                {
                    yield return startsWithProxy.Create(createEvent);
                }
            }
        }
    }

    extension(HandlesEventRegistration[] registrations)
    {
        public IHandlesProxy<TEntity> GetProxy<TEntity>(IEvent @event)
        {
            var handles = registrations.Single(x => x.EntityType == typeof(TEntity) && x.EventType == @event.GetType());
            var proxyType = typeof(HandlesProxy<,>).MakeGenericType(handles.EventType, typeof(TEntity));
            return Activator.CreateInstance(proxyType) as IHandlesProxy<TEntity> ?? throw new InvalidOperationException("Failed to create proxy");
        }

        public IEntity Handle(IEvent @event, IEntity entity)
        {
            var handles = registrations.Single(x => x.EntityType == entity.GetType() && x.EventType == @event.GetType());
            var proxyType = typeof(HandlesProxy<,>).MakeGenericType(handles.EventType, entity.GetType());
            if (Activator.CreateInstance(proxyType) is IHandlesProxy handlesProxy)
            {
                return handlesProxy.Handle(@event, entity);
            }
            else
            {
                return entity;
            }
        }
    }

    // TODO: convert to proxy
    //extension(EventListenerRegistration[] registrations)
    //{
    //    public async Task BroadcastEventAsync(IServiceProvider sp, IEvent @event, IEntity entity)
    //    {
    //        var eventEntityListeners = registrations.Where(x => x.EventType == @event.GetType() && x.EntityType == entity.GetType());
    //        foreach (var reg in eventEntityListeners)
    //        {
    //            var listener = typeof(IEventListener<,>).MakeGenericType(@event.GetType(), entity.GetType());
    //            var method = listener.GetMethod(nameof(IEventListener<,>.HandleAsync));
    //            await (Task)(method!.Invoke(sp.GetRequiredService(reg.Listener), [@event, entity]));
    //        }
    //    }
    //}

    extension(IServiceProvider serviceProvider)
    {
        public async Task BroadcastEventAsync(IEvent @event, IEntity entity)
        {
            var handlers = serviceProvider.GetKeyedServices<IEventListenerProxy>((@event.GetType(), entity.GetType()));
            foreach (var handler in handlers)
            {
                await handler.HandleAsync(@event, entity);
            }
        }
    }
}
