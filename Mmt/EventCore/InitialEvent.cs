namespace EventCore;

public sealed record InitialEvent(Guid Id, string Name) : ICreateEvent;

