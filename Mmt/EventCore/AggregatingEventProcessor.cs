using System.Threading.Channels;
using Microsoft.Extensions.Hosting;

namespace EventCore;

internal class AggregatingEventProcessor : BackgroundService
{
    private readonly ChannelReader<IEvent> _eventChannel;
    private readonly ChannelWriter<EventEntity> _entityChannel;
    private readonly StartsWithEventRegistration[] _startsWithEventRegistrations;
    private readonly HandlesEventRegistration[] _handlesEventRegistrations;

    private readonly Dictionary<Guid, IEntity> _entities = new();

    public AggregatingEventProcessor(
        ChannelReader<IEvent> eventChannel,
        ChannelWriter<EventEntity> entityChannel,
        IEnumerable<StartsWithEventRegistration> startsWithEventRegistrations,
        IEnumerable<HandlesEventRegistration> handlesEventRegistrations)
    {
        _eventChannel = eventChannel;
        _entityChannel = entityChannel;
        _startsWithEventRegistrations = startsWithEventRegistrations.ToArray();
        _handlesEventRegistrations = handlesEventRegistrations.ToArray();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var @event in _eventChannel.ReadAllAsync(stoppingToken))
        {
            if (@event is ICreateEvent createEvent)
            {
                var entities = _startsWithEventRegistrations.CreateEntities(createEvent);
                foreach (var entity in entities)
                {
                    _entities[entity.Id] = entity;
                    await _entityChannel.WriteAsync(new(@event, entity));
                }
            }
            else if (_entities.TryGetValue(@event.Id, out var entity))
            {
                _entities[@event.Id] = _handlesEventRegistrations.Handle(@event, entity);
                await _entityChannel.WriteAsync(new(@event, entity));
            }

            // log about this?
        }
    }
}
