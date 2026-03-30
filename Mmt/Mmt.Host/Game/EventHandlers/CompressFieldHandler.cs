using System.Threading.Channels;
using EventCore;
using Mmt.Host.Game.AudioEvents;
using Mmt.Host.Game.Events;
using Mmt.Host.Models;
using ISession = EventCore.ISession;

namespace Mmt.Host.Game.EventHandlers;

public class CompressFieldHandler : IEventListener<CompressField, GameEntity>
{
    private readonly ISession _session;
    private readonly ChannelWriter<AudioEvent> _audioChannel;

    public CompressFieldHandler(
        ISession session,
        ChannelWriter<AudioEvent> audioChannel)
    {
        _session = session;
        _audioChannel = audioChannel;
    }

    public async Task HandleAsync(CompressField @event, GameEntity entity)
    {
        _audioChannel.TryWrite(new AudioEvent
        {
            PlayerIds = null,
            Type = AudioType.LineRemoved
        });

        var blocksToRemove = new List<Position>();

        var size = entity.Size / entity.TileSize;
        for (var y = 0; y < size; y++)
        {
            var percentage = (y / size);

            blocksToRemove.AddRange(Enumerable.Range(0, size)
                .Where(_ => Random.Shared.NextDouble() > percentage)
                .Select(x => new Position(x, y)));
        }

        await _session.Events.AppendAsync(new RemoveBlocks(entity.Id, blocksToRemove.ToArray()));

        foreach (var player in entity.Players)
        {
            await _session.Events.AppendAsync(new UpdatePlayerHealth(entity.Id, player.Id, -10));
        }
    }
}
