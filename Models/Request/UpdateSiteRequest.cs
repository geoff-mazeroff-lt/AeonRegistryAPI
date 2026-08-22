using System.ComponentModel.DataAnnotations;

namespace AeonRegistryAPI.Models.Request;

public record UpdateSiteRequest
{
    [Required, MaxLength(200)]
    public string? Name { get; set; }
    
    [Required, MaxLength(500)]
    public string? Location { get; set; }
    
    [MaxLength(100)]
    public string? Coordinates { get; set; }
    
    public double Latitude { get; set; }
    
    public double Longitude { get; set; }
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [MaxLength(2000)]
    public string? PublicNarrative { get; set; }
    
    [MaxLength(2000)]
    public string? AeonNarrative { get; set; }
}