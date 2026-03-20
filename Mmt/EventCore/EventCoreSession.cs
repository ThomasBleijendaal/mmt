namespace EventCore;

internal class EventCoreSession : ISession
{
    public EventCoreSession(
        EventStoreOperations eventStoreOperations,
        EntityCache entityCache)
    {
        Events = eventStoreOperations;
        EntityCache = entityCache;
    }

    public IEventStoreOperations Events { get; }

    public IEntityCache EntityCache { get; }
}
