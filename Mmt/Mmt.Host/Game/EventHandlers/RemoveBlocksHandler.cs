using System.Threading.Channels;
using EventCore;
using Mmt.Host.Game.AudioEvents;
using Mmt.Host.Game.Events;
using ISession = EventCore.ISession;

namespace Mmt.Host.Game.EventHandlers;

public class RemoveBlocksHandler : IEventListener<RemoveBlocks, GameEntity>
{
    private readonly ISession _session;
    private readonly ChannelWriter<AudioEvent> _audioChannel;

    public RemoveBlocksHandler(
        ISession session,
        ChannelWriter<AudioEvent> audioChannel)
    {
        _session = session;
        _audioChannel = audioChannel;
    }

    public async Task HandleAsync(RemoveBlocks @event, GameEntity entity)
    {
        var size = entity.Size / entity.TileSize;
        var completeRows = @event.Blocks.GroupBy(x => x.Y).Count(r => r.Count() == size);

        if (completeRows > 0)
        {
            if ((entity.RowsCleared / 10) != ((entity.RowsCleared - completeRows) / 10))
            {
                foreach (var player in entity.Players)
                {
                    await _session.Events.AppendAsync(new UpdatePlayerHealth(@event.Id, player.Id, 20));
                }
            }

            _audioChannel.TryWrite(new AudioEvent
            {
                PlayerIds = null,
                Type = AudioType.LineRemoved
            });
        }
    }
}
