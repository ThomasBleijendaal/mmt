namespace EventCore;

public sealed record SequentialEvent(Guid Id, string Name) : IEvent;

