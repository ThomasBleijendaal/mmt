namespace EventCore;

public interface IEntityCache
{
    TEntity[] GetActiveEntities<TEntity>()
        where TEntity : class, IEntity;
}
