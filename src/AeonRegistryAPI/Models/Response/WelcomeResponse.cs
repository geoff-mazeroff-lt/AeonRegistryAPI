namespace AeonRegistryAPI.Models.Response;

public record WelcomeResponse
{
    public string? Message { get; init; }
    public string? Version { get; init; }
    public string? TimeOnly { get; init; }
}