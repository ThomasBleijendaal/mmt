namespace EventCore;

internal class HandlesProxy<TEvent, TEntity> : IHandlesProxy<TEntity>, IHandlesProxy
    where TEntity : IHandles<TEvent, TEntity>, IEntity
    where TEvent : IEvent
{
    public TEntity Handle(IEvent @event, TEntity entity) => TEntity.Handle((TEvent)@event, entity);

    public IEntity Handle(IEvent @event, IEntity entity) => TEntity.Handle((TEvent)@event, (TEntity)entity);
}
