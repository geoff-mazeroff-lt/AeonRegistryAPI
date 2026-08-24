using System.ComponentModel.DataAnnotations;

namespace AeonRegistryAPI.Endpoints.CustomIdentity.Models;

public record RegisterUserRequest
{
    [Required]
    public required string Email { get; init; }
    
    [Required]
    public required string FirstName { get; init; }

    [Required]
    public required string LastName { get; init; }
}