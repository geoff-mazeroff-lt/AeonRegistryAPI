namespace AeonRegistryAPI.Models.Response;

public record PublicArtifactImageResponse
{
    public byte[] Data { get; set; } = [];
    public string ContentType { get; set; } = "image/png";
}