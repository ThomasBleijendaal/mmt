using System.Threading.Channels;
using Mmt.Host.Game;
using Mmt.Host.Models;

namespace Mmt.Host;

public static class Extensions
{
    extension(int[] input)
    {
        public Position ToPosition() => new Position(input[0], input[1]);
    }

    extension(int[][] input)
    {
        public Position[] ToPositions() => [.. input.Select(d => d.ToPosition())];
    }

    extension(IEnumerable<Block> blocks)
    {
        public bool RowFull(int width, int tileSize)
        {
            var maxEmpty = (2 * (4 - tileSize));
            return blocks.Take(width).Count(x => x.IsEmpty) <= maxEmpty;
        }
    }

    extension(List<List<Block>> list)
    {
        public string? GetColor(Position pos)
        {
            if (pos.X < 0 || pos.X >= list[0].Count ||
                pos.Y < 0 || pos.Y >= list.Count)
            {
                return null;
            }

            return list[pos.Y][pos.X].Color;
        }

        public void SetColor(Position pos, string? color)
        {
            if (pos.X < 0 || pos.X >= list[0].Count ||
                pos.Y < 0 || pos.Y >= list.Count)
            {
                return;
            }

            list[pos.Y][pos.X] = list[pos.Y][pos.X] with { Color = color };
        }

        public void SetColor(IEnumerable<Position> pos, string? color)
        {
            foreach (var p in pos)
            {
                list.SetColor(p, color);
            }
        }

        public IEnumerable<Block> Blocks => list.GetAllBlocks();

        private IEnumerable<Block> GetAllBlocks()
        {
            foreach (var row in list)
            {
                foreach (var block in row)
                {
                    yield return block;
                }
            }
        }
    }

    extension(GameEntity entity)
    {
        public Guid[] PlayerIdsExcept(Guid playerId) => entity.Players.Select(x => x.Id).Except([playerId]).ToArray();
    }

    extension<T>(ChannelReader<T> channel)
    {
        public T[] ReadAvailable()
        {
            var result = new List<T>(20);
            while (channel.TryRead(out var item))
            {
                result.Add(item);
            }

            return result.ToArray();
        }
    }
}
