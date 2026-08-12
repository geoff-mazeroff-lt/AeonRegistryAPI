using System.ComponentModel.DataAnnotations;

namespace AeonRegistryAPI.Endpoints.CustomIdentity.Models;

public record UpdateUserProfileRequest
{
    [Required]
    public string? FirstName { get; init; }
    
    [Required]
    public string? LastName { get; init; }
}