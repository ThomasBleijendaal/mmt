namespace EventCore;

internal class EventListenerProxy<TEvent, TEntity> : IEventListenerProxy
    where TEvent : IEvent
    where TEntity : IEntity
{
    private readonly IEventListener<TEvent, TEntity> _listener;

    public EventListenerProxy(
        IEventListener<TEvent, TEntity> listener)
    {
        _listener = listener;
    }

    public async Task HandleAsync(IEvent @event, IEntity entity)
    {
        await _listener.HandleAsync((TEvent)@event, (TEntity)@entity);
    }
}
