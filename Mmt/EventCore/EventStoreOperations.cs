using System.Threading.Channels;

namespace EventCore;

internal class EventStoreOperations : IEventStoreOperations
{
    private readonly IEventStore _eventStore;
    private readonly ChannelWriter<IEvent> _eventChannel;
    private readonly StartsWithEventRegistration[] _startsWithEventRegistrations;
    private readonly HandlesEventRegistration[] _handlesEventRegistrations;

    public EventStoreOperations(
        IEventStore eventStore,
        IEnumerable<StartsWithEventRegistration> startsWithEventRegistrations,
        IEnumerable<HandlesEventRegistration> handlesEventRegistrations,
        ChannelWriter<IEvent> eventChannel)
    {
        _eventStore = eventStore;
        _eventChannel = eventChannel;
        _startsWithEventRegistrations = startsWithEventRegistrations.ToArray();
        _handlesEventRegistrations = handlesEventRegistrations.ToArray();
    }

    public async Task StartStreamAsync<TEvent>(TEvent @event) where TEvent : IEvent
    {
        await _eventStore.StoreEventAsync(@event);
        await _eventChannel.WriteAsync(@event);
    }

    public async Task AppendAsync<TEvent>(TEvent @event) where TEvent : IEvent
    {
        await _eventStore.StoreEventAsync(@event);
        await _eventChannel.WriteAsync(@event);
    }

    public async Task<TEntity?> AggregateStreamAsync<TEntity>(Guid id) where TEntity : IEntity
    {
        var events = await _eventStore.GetEventsAsync(id);
        if (events.Length == 0)
        {
            return default;
        }

        var createProxy = _startsWithEventRegistrations.GetProxy<TEntity>();

        var entity = createProxy.Create(events.OfType<ICreateEvent>().First());

        foreach (var @event in events[1..])
        {
            var handlesProxy = _handlesEventRegistrations.GetProxy<TEntity>(@event);
            entity = handlesProxy.Handle(@event, entity);
        }

        return entity;
    }
}
