namespace EventCore;

internal class HandlesProxy<TEntity, TCommand> : IHandlesProxy<TEntity>, IHandlesProxy
    where TEntity : IHandles<TEntity, TCommand>, IEntity
    where TCommand : IEvent
{
    public TEntity Handle(IEvent @event, TEntity entity) => TEntity.Handle((TCommand)@event, entity);

    public IEntity Handle(IEvent @event, IEntity entity) => TEntity.Handle((TCommand)@event, (TEntity)entity);
}

