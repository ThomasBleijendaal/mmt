namespace EventCore;

public interface IEventListener;

public interface IEventListener<TEvent, TEntity> : IEventListener
    where TEvent : IEvent
    where TEntity : IEntity
{
    Task HandleAsync(TEvent @event, TEntity entity);
}
