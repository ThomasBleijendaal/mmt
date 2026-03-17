namespace EventCore;

public record Entity :
    IEntity,
    IStartsWith<Entity, InitialEvent>,
    IHandles<Entity, SequentialEvent>
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public static Entity Create(InitialEvent command) => new() { Id = command.Id, Name = command.Name };

    public static Entity Handle(SequentialEvent command, Entity current) => current with { Name = command.Name };
}

