namespace AeonRegistryAPI.Endpoints.CustomIdentity.Models;

public record UserProfileResponse
{
    public string? Id { get; init; }
    public string? Email { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? FullName { get; init; }
}