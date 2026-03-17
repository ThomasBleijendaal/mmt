namespace EventCore;

internal interface IHandlesProxy<TEntity>
{
    TEntity Handle(IEvent @event, TEntity entity);
}

internal interface IHandlesProxy
{
    IEntity Handle(IEvent @event, IEntity entity);
}
