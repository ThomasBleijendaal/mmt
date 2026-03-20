using EventCore;

namespace Mmt.Host.Game.Events;

public record UpdateGameStatus(Guid Id, GameStatus Status) : IEvent;
