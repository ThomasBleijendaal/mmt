using System.Collections.Concurrent;
using System.Net.WebSockets;
using Mmt.Host.Game.Events;

namespace Mmt.Host.WebSockets;

internal class WebSocketHandler
{
    private readonly ConcurrentDictionary<WebSocket, WebSocketRegistration> _webSockets = new();
    private readonly EventCore.ISession _session;

    public WebSocketHandler(EventCore.ISession session)
    {
        _session = session;
    }

    public Task AddWebSocketAsync(Guid gameId, Guid playerId, WebSocket ws)
    {
        Console.WriteLine($"Adding {ws.GetHashCode()}");

        var tcs = new TaskCompletionSource();

        _webSockets[ws] = new(ws, PlayerId: playerId, GameId: gameId, tcs);

        return tcs.Task;
    }

    public IEnumerable<(Guid guidId, Guid playerId, WebSocket ws)> GetAllWebSockets(Guid? gameId)
    {
        var wss = gameId.HasValue
            ? _webSockets.Where(x => x.Value.GameId == gameId).ToArray()
            : _webSockets.ToArray();

        foreach (var (_, (ws, playerId, playerGameId, _)) in wss)
        {
            if (ws.State == WebSocketState.Open)
            {
                yield return (playerGameId, playerId, ws);
            }
            else
            {
                // removing does not need to be awaited
                _ = RemoveWebSocketAsync(ws);
            }
        }
    }

    public WebSocket? GetWebSocket(Guid gameId, Guid playerId)
    {
        return _webSockets.Values.FirstOrDefault(x => x.GameId == gameId && x.PlayerId == playerId)?.WebSocket;
    }

    public async Task RemoveWebSocketAsync(WebSocket ws)
    {
        Console.WriteLine($"Removing {ws.GetHashCode()}");

        if (ws.State != WebSocketState.Closed)
        {
            try
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, null, default);
            }
            catch
            {
            }
        }

        if (_webSockets.TryRemove(ws, out var registration))
        {
            await _session.Events.AppendAsync(new DropPlayer(registration.GameId, registration.PlayerId));
            registration.TaskCompletionSource.SetResult();
        }
    }

    private sealed record WebSocketRegistration(WebSocket WebSocket, Guid PlayerId, Guid GameId, TaskCompletionSource TaskCompletionSource);
}
