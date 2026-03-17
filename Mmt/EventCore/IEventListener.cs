namespace EventCore;

public interface IEventListener<TEvent>
{
    Task HandleAsync(TEvent @event);
}

