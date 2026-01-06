
using System.ComponentModel.DataAnnotations;

namespace Mmt.Host.Game;

public record PlayerJoinRequest
{
    [Required]
    public required string Name { get; init; }

    [Required]
    public required string Color { get; init; }
}
