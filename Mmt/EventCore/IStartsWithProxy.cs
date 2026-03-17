namespace EventCore;

internal interface IStartsWithProxy<TEntity>
{
    TEntity Create(ICreateEvent @event);
}

internal interface IStartsWithProxy
{
    IEntity Create(ICreateEvent createEvent);
}

