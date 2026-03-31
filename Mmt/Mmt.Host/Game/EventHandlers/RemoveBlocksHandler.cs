using System.Threading.Channels;
using EventCore;
using Mmt.Host.Game.AudioEvents;
using Mmt.Host.Game.Events;
using Mmt.Host.Game.VisualEvents;
using ISession = EventCore.ISession;

namespace Mmt.Host.Game.EventHandlers;

public class RemoveBlocksHandler : IEventListener<RemoveBlocks, GameEntity>
{
    private readonly ISession _session;
    private readonly ChannelWriter<AudioEvent> _audioChannel;
    private readonly ChannelWriter<VisualEvent> _visualChannel;

    public RemoveBlocksHandler(
        ISession session,
        ChannelWriter<AudioEvent> audioChannel,
        ChannelWriter<VisualEvent> visualChannel)
    {
        _session = session;
        _audioChannel = audioChannel;
        _visualChannel = visualChannel;
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

        _visualChannel.TryWrite(new BlockRemoved
        {
            PlayerIds = null,
            Positions = @event.Blocks
        });
    }
}
