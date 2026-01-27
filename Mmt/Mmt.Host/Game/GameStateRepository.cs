using System.Collections.Concurrent;

namespace Mmt.Host.Game;

public class GameStateRepository
{
    private readonly ConcurrentDictionary<Guid, GameState> _states = new();
    private readonly int _size;

    public GameStateRepository(int size)
    {
        _size = size;
    }

    public GameState GetGame(Guid gameId)
    {
        return _states.GetOrAdd(gameId, _ => new GameState(_size));
    }

    public IEnumerable<(Guid id, GameState state)> GetGameIds()
    {
        var states = _states.ToArray();

        foreach (var (gameId, state) in states)
        {
            if (state.PlayerCount == 0)
            {
                _states.Remove(gameId, out _);
            }
            else
            {
                yield return (gameId, state);
            }
        }
    }
}
