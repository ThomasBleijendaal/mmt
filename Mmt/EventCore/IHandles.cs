namespace EventCore;

public interface IHandles<T, TCommand>
    where TCommand : IEvent
{
    static abstract T Handle(TCommand command, T current);
}
