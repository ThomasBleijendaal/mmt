namespace EventCore;

public interface IStartsWith<TEvent, TEntity>
    where TEvent : ICreateEvent
    where TEntity : IEntity
{
    static abstract TEntity Create(TEvent command);
}
