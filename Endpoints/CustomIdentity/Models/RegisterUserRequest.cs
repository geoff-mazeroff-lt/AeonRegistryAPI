using System.ComponentModel.DataAnnotations;

namespace AeonRegistryAPI.Endpoints.CustomIdentity.Models;

public record RegisterUserRequest
{
    [Required]
    public string Email { get; init; }
    
    [Required]
    public string FirstName { get; init; }

    [Required]
    public string LastName { get; init; }
}