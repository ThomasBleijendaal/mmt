using System.Threading.Channels;
using Microsoft.Extensions.Hosting;

namespace EventCore;

internal class AggregatingEventProcessor : BackgroundService
{
    private readonly ChannelReader<IEvent> _eventChannel;
    private readonly EntityCache _entityCache;
    private readonly IServiceProvider _serviceProvider;
    private readonly StartsWithEventRegistration[] _startsWithEventRegistrations;
    private readonly HandlesEventRegistration[] _handlesEventRegistrations;


    public AggregatingEventProcessor(
        ChannelReader<IEvent> eventChannel,
        EntityCache entityCache,
        IServiceProvider serviceProvider,
        IEnumerable<StartsWithEventRegistration> startsWithEventRegistrations,
        IEnumerable<HandlesEventRegistration> handlesEventRegistrations)
    {
        _eventChannel = eventChannel;
        _entityCache = entityCache;
        _serviceProvider = serviceProvider;
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
                    _entityCache.SetEntity(entity);
                    await _serviceProvider.BroadcastEventAsync(@event, entity);
                }
            }
            else if (_entityCache.GetEntity(@event.Id) is IEntity entity)
            {
                _entityCache.SetEntity(_handlesEventRegistrations.Handle(@event, entity));
                await _serviceProvider.BroadcastEventAsync(@event, entity);
            }

            // log about this?
        }
    }
}
