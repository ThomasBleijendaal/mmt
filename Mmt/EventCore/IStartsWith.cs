namespace EventCore;

public interface IStartsWith<T, TCommand>
    where TCommand : ICreateEvent
{
    static abstract T Create(TCommand command);
}

