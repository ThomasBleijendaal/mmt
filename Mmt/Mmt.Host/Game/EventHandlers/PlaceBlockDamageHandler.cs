using System.Threading.Channels;
using EventCore;
using Mmt.Host.Game.AudioEvents;
using Mmt.Host.Game.Events;
using ISession = EventCore.ISession;

namespace Mmt.Host.Game.EventHandlers;

public class PlaceBlockDamageHandler : IEventListener<PlaceBlock, GameEntity>
{
    private readonly ISession _session;
    private readonly ChannelWriter<AudioEvent> _audioChannel;

    public PlaceBlockDamageHandler(
        ISession session,
        ChannelWriter<AudioEvent> audioChannel)
    {
        _session = session;
        _audioChannel = audioChannel;
    }

    public async Task HandleAsync(PlaceBlock @event, GameEntity entity)
    {
        if (@event.Positions.All(x => x.Y <= 3))
        {
            await _session.Events.AppendAsync(new UpdatePlayerHealth(@event.Id, @event.PlayerId, -3));

            _audioChannel.TryWrite(new AudioEvent
            {
                PlayerIds = [@event.PlayerId],
                Type = AudioType.BlockPlacedFailed
            });

            if (entity.InvalidBlocksPlaced > 10)
            {
                await _session.Events.AppendAsync(new CompressField(@event.Id));
            }
        }
        else
        {
            _audioChannel.TryWrite(new AudioEvent
            {
                PlayerIds = entity.PlayerIdsExcept(@event.PlayerId),
                Type = AudioType.BlockPlaced
            });
        }
    }
}
