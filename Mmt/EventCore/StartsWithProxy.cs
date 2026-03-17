namespace EventCore;

internal class StartsWithProxy<TEntity, TCommand> : IStartsWithProxy<TEntity>, IStartsWithProxy
    where TEntity : IStartsWith<TEntity, TCommand>, IEntity
    where TCommand : ICreateEvent
{
    public TEntity Create(ICreateEvent createEvent) => TEntity.Create((TCommand)createEvent);

    IEntity IStartsWithProxy.Create(ICreateEvent createEvent) => TEntity.Create((TCommand)createEvent);
}

