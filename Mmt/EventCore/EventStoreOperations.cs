using System.Threading.Channels;

namespace EventCore;

internal class EventStoreOperations : IEventStoreOperations
{
    private readonly IEventStore _eventStore;
    private readonly IServiceProvider _serviceProvider;
    private readonly ChannelWriter<IEvent> _eventChannel;

    public EventStoreOperations(
        IEventStore eventStore,
        IServiceProvider serviceProvider,
        ChannelWriter<IEvent> eventChannel)
    {
        _eventStore = eventStore;
        _serviceProvider = serviceProvider;
        _eventChannel = eventChannel;
    }

    public async Task StartStreamAsync<TEvent>(TEvent @event) where TEvent : ICreateEvent
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

        var createEvent = events.OfType<ICreateEvent>().First();
        var createProxy = _serviceProvider.GetProxy<TEntity>(createEvent);

        var entity = createProxy.Create(createEvent);

        foreach (var @event in events[1..])
        {
            var handlesProxy = _serviceProvider.GetProxy<TEntity>(@event);
            entity = handlesProxy.Handle(@event, entity);
        }

        return entity;
    }
}
