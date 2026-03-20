namespace EventCore;

public interface IHandles<TEvent, TEntity>
    where TEvent : IEvent
    where TEntity : IEntity
{
    static abstract TEntity Handle(TEvent command, TEntity current);
}
