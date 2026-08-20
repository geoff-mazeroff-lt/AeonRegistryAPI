namespace AeonRegistryAPI.Models.Response;

public record PublicArtifactMediaResponse
{
    public byte[] Data { get; set; } = [];
    public string ContentType { get; set; } = "image/png";
}