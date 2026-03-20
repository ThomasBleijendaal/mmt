namespace EventCore;

internal interface IEventListenerProxy
{
    Task HandleAsync(IEvent @event, IEntity entity);
}
