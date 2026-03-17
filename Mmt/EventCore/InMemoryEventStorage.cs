namespace EventCore;

internal class InMemoryEventStorage : IEventStore
{
    private readonly Dictionary<Guid, List<IEvent>> _storage = new();

    public Task<IEvent[]> GetEventsAsync(Guid id) => Task.FromResult(_storage.TryGetValue(id, out var events) ? events.ToArray() : []);

    public Task StoreEventAsync<TEvent>(TEvent @event) where TEvent : IEvent
    {
        if (_storage.TryGetValue(@event.Id, out var value))
        {
            value.Add(@event);
        }
        else
        {
            _storage[@event.Id] = [@event];
        }

        return Task.CompletedTask;
    }
}
