namespace EventCore;

public interface IEventStore
{
    Task StoreEventAsync<TEvent>(TEvent @event) where TEvent : IEvent;
    Task<IEvent[]> GetEventsAsync(Guid id);
}

