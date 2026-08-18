namespace AeonRegistryAPI.Models.Response;

public class PrivateSiteResponse
{
    public int Id { get; set; }
    public string? Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Coordinates { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Description { get; set; }
    public string? PublicNarrative { get; set; }
    public string? AeonNarrative { get; set; }
}