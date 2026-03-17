namespace EventCore;

public interface ISession
{
    IEventStoreOperations Events { get; }
}

