using System.Threading.Channels;
using Microsoft.Extensions.Hosting;

namespace EventCore;

internal class AggregatingEventProcessor : BackgroundService
{
    private readonly ChannelReader<IEvent> _eventChannel;
    private readonly EntityCache _entityCache;
    private readonly IServiceProvider _serviceProvider;


    public AggregatingEventProcessor(
        ChannelReader<IEvent> eventChannel,
        EntityCache entityCache,
        IServiceProvider serviceProvider)
    {
        _eventChannel = eventChannel;
        _entityCache = entityCache;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var @event in _eventChannel.ReadAllAsync(stoppingToken))
        {
            if (@event is ICreateEvent createEvent)
            {
                var entities = _serviceProvider.CreateEntities(createEvent);
                foreach (var entity in entities)
                {
                    _entityCache.SetEntity(entity);
                    await _serviceProvider.BroadcastEventAsync(@event, entity);
                }
            }
            else if (_entityCache.GetEntity(@event.Id) is IEntity entity)
            {
                _entityCache.SetEntity(_serviceProvider.Handle(@event, entity));
                await _serviceProvider.BroadcastEventAsync(@event, entity);
            }

            // log about this?
        }
    }
}
