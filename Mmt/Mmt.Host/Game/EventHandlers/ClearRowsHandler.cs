using System.Collections.Immutable;
using EventCore;
using Mmt.Host.Game.Events;
using Mmt.Host.Models;
using ISession = EventCore.ISession;

namespace Mmt.Host.Game.EventHandlers;

public class ClearRowsHandler : IEventListener<PlaceBlock, GameEntity>
{
    private readonly ISession _session;

    public ClearRowsHandler(ISession session)
    {
        _session = session;
    }

    public async Task HandleAsync(PlaceBlock @event, GameEntity entity)
    {
        var width = entity.Size / entity.TileSize;
        for (var r = entity.Field.Count - 1; r >= 0; r--)
        {
            if (entity.Field[r].RowFull(width, entity.TileSize))
            {
                var rowsComplete = 1;
                while (rowsComplete <= r && entity.Field[r - rowsComplete].RowFull(width, entity.TileSize))
                {
                    rowsComplete++;
                }

                var filledBlocks = entity.Field.Skip(r - rowsComplete + 1).Take(rowsComplete).SelectMany(x => x).ToArray();
                var totalBlocks = (double)filledBlocks.Length;

                var percentages = filledBlocks
                    .Where(x => x.Color != null)
                    .GroupBy(x => x.Color)
                    .Select(g => (color: g.Key, percentage: g.Count() / totalBlocks))
                    .OrderByDescending(d => d.percentage)
                    .ToArray();

                var maxPercentage = percentages.Max(x => x.percentage);

                var colorsInBlocks = percentages.Select(x => x.color).ToArray();

                foreach (var (color, percentage) in percentages)
                {
                    var damage = percentage switch
                    {
                        _ when percentage < maxPercentage / 4.0 => 3,
                        _ when percentage < maxPercentage / 3.0 => 2,
                        _ when percentage < maxPercentage / 2.0 => 1,
                        _ => 0
                    };

                    var player = entity.Players.Single(x => x.Color == color);

                    await _session.Events.AppendAsync(new UpdatePlayerHealth(@event.Id, player.Id, damage));
                }

                var playersNotInBlocks = entity.Players.Except(entity.Players.Where(p => colorsInBlocks.Contains(p.Color)));
                foreach (var player in playersNotInBlocks)
                {
                    await _session.Events.AppendAsync(new UpdatePlayerHealth(@event.Id, player.Id, -4));
                }

                var blocksInRows = Enumerable.Range(r, rowsComplete).SelectMany(y => Enumerable.Range(0, width).Select(x => new Position(x, y))).ToArray();
                await _session.Events.AppendAsync(new RemoveBlocks(@event.Id, blocksInRows));
            }
        }
    }
}
