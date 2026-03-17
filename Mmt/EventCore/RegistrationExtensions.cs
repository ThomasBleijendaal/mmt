namespace EventCore;

// TODO: cache these proxies
internal static class RegistrationExtensions
{
    extension(StartsWithEventRegistration[] registrations)
    {
        public IStartsWithProxy<TEntity> GetProxy<TEntity>()
        {
            var startsWith = registrations.Single(x => x.EntityType == typeof(TEntity));
            var proxyType = typeof(StartsWithProxy<,>).MakeGenericType(typeof(TEntity), startsWith.EventType);
            return Activator.CreateInstance(proxyType) as IStartsWithProxy<TEntity> ?? throw new InvalidOperationException("Failed to create proxy");
        }

        public IEnumerable<IEntity> CreateEntities(ICreateEvent createEvent)
        {
            var creators = registrations.Where(x => x.EventType == createEvent.GetType());
            foreach (var handles in creators)
            {
                var proxyType = typeof(StartsWithProxy<,>).MakeGenericType(handles.EntityType, handles.EventType);
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
            var proxyType = typeof(HandlesProxy<,>).MakeGenericType(typeof(TEntity), handles.EventType);
            return Activator.CreateInstance(proxyType) as IHandlesProxy<TEntity> ?? throw new InvalidOperationException("Failed to create proxy");
        }

        public IEntity Handle(IEvent @event, IEntity entity)
        {
            var handles = registrations.Single(x => x.EntityType == entity.GetType() && x.EventType == @event.GetType());
            var proxyType = typeof(HandlesProxy<,>).MakeGenericType(entity.GetType(), handles.EventType);
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
}
