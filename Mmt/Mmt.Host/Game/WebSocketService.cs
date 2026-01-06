using System.Net.WebSockets;

namespace Mmt.Host.Game;

public class WebSocketService
{
    private readonly Dictionary<Guid, WebSocket> _sockets = [];

    public void Add(Guid playerId, WebSocket ws) => _sockets[playerId] = ws;

    public void Remove(Guid playerId) => _sockets.Remove(playerId);

    public IEnumerable<(Guid playerId, WebSocket ws)> GetWebSockets()
    {
        var data = _sockets.ToArray();
        foreach (var (playerId, ws) in data)
        {
            if (ws.State == WebSocketState.Open)
            {
                yield return (playerId, ws);
            }
            else
            {
                _sockets.Remove(playerId);
            }
        }
    }
}
