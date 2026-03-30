namespace EventCore;

internal class EntityCache : IEntityCache
{
    private readonly Dictionary<Guid, IEntity> _entities = new();

    public TEntity[] GetActiveEntities<TEntity>() where TEntity : class, IEntity
    {
        return _entities.Values.OfType<TEntity>().ToArray();
    }

    public IEntity? GetEntity(Guid id)
    {
        if (_entities.TryGetValue(id, out var entity))
        {
            return entity;
        }

        return default;
    }

    public void SetEntity(IEntity entity)
    {
        _entities[entity.Id] = entity;
    }
}
