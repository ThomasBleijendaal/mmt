namespace EventCore;

public interface IEventStoreOperations
{
    Task StartStreamAsync<TEvent>(TEvent @event) where TEvent : ICreateEvent;

    Task AppendAsync<TEvent>(TEvent @event) where TEvent : IEvent;

    Task<TEntity?> AggregateStreamAsync<TEntity>(Guid id) where TEntity : IEntity;
}
