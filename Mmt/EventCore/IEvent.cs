namespace EventCore;

public interface IEvent
{
    Guid Id { get; }
}

public interface ICreateEvent : IEvent
{
}

