using System.Threading.Channels;
using EventCore;
using Mmt.Host.Game.AudioEvents;
using Mmt.Host.Game.Events;
using ISession = EventCore.ISession;

namespace Mmt.Host.Game.EventHandlers;

public class BoardSizeHandler :
    IEventListener<JoinGame, GameEntity>,
    IEventListener<DropPlayer, GameEntity>,
    IEventListener<UpdatePlayerHealth, GameEntity>
{
    private readonly ISession _session;
    private readonly ChannelWriter<AudioEvent> _audioChannel;

    public BoardSizeHandler(
        ISession session,
        ChannelWriter<AudioEvent> audioChannel)
    {
        _session = session;
        _audioChannel = audioChannel;
    }

    public Task HandleAsync(JoinGame @event, GameEntity entity) => UpdateBoardSizeAsync(entity);
    public Task HandleAsync(DropPlayer @event, GameEntity entity) => UpdateBoardSizeAsync(entity);
    public Task HandleAsync(UpdatePlayerHealth @event, GameEntity entity) => UpdateBoardSizeAsync(entity);

    private async Task UpdateBoardSizeAsync(GameEntity entity)
    {
        var requiredTileSize = entity.Players.Count(p => p.Health > 0) switch
        {
            var x when x > 8 => 1,
            var x when x > 4 => 2,
            var x when x > 2 => 3,
            _ => 4
        };

        if (requiredTileSize != entity.TileSize)
        {
            await _session.Events.AppendAsync(new ResizeBoard(entity.Id, requiredTileSize));

            _audioChannel.TryWrite(new AudioEvent
            {
                PlayerIds = null,
                Type = AudioType.LineRemoved
            });
        }
    }
}
