namespace EventCore;

internal class StartsWithProxy<TEvent, TEntity> : IStartsWithProxy<TEntity>, IStartsWithProxy
    where TEntity : IStartsWith<TEvent, TEntity>, IEntity
    where TEvent : ICreateEvent
{
    public TEntity Create(ICreateEvent createEvent) => TEntity.Create((TEvent)createEvent);

    IEntity IStartsWithProxy.Create(ICreateEvent createEvent) => TEntity.Create((TEvent)createEvent);
}
