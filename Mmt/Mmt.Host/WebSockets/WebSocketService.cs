using System.Collections.Concurrent;
using System.Net.WebSockets;
using Mmt.Host.Game;

namespace Mmt.Host.WebSockets;

internal class WebSocketHandler
{
    private readonly ConcurrentDictionary<WebSocket, WebSocketRegistration> _webSockets = new();
    private readonly GameState _gameState;

    public WebSocketHandler(
        GameState gameState)
    {
        _gameState = gameState;
    }

    public Task AddWebSocketAsync(Guid id, WebSocket ws)
    {
        Console.WriteLine($"Adding {ws.GetHashCode()}");

        var tcs = new TaskCompletionSource();

        _webSockets[ws] = new(ws, id, tcs);

        return tcs.Task;
    }

    public IEnumerable<(Guid id, WebSocket ws)> GetAllWebSockets()
    {
        var wss = _webSockets.ToArray();

        foreach (var (_, (ws, id, _)) in wss)
        {
            if (ws.State == WebSocketState.Open)
            {
                yield return (id, ws);
            }
            else
            {
                // removing does not need to be awaited
                _ = RemoveWebSocketAsync(ws);
            }
        }
    }

    public WebSocket? GetWebSocket(Guid id)
    {
        return _webSockets.Values.FirstOrDefault(x => x.Id == id)?.WebSocket;
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
            _gameState.DropPlayer(registration.Id);
            registration.TaskCompletionSource.SetResult();
        }
    }

    public record WebSocketRegistration(WebSocket WebSocket, Guid Id, TaskCompletionSource TaskCompletionSource);
}
