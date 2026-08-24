namespace AeonRegistryAPI.Models.Response;

public record PrivateArtifactResponse
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? CatalogNumber { get; set; } 
    public string? Description { get; set; } 
    public string? PrivateDescription { get; set; }
    public DateTime DateDiscovered { get; set; }
    public string? Type { get; set; }
    public string? SiteName { get; set; } 
    public string? PrimaryImageUrl { get; set; }
}