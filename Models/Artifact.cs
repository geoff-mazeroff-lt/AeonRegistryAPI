using System.ComponentModel.DataAnnotations;

namespace AeonRegistryAPI.Models;

public class Artifact
{
    public int Id { get; set; }
    
    [Required, MaxLength(200)]
    public string? Name { get; set; }
    
    [Required, MaxLength(500)]
    public string? CatalogNumber { get; set; }
    
    [MaxLength(2000)]
    public string? Description { get; set; }
    
    [MaxLength(2000)]
    public string? PublicNarrative { get; set; }
    
    public DateTime DateDiscovered { get; set; }
    
    public string? Type { get; set; }
    
    [Required]
    public int SiteId { get; set; }
    public Site? Site { get; set; }
    
    public ICollection<ArtifactMediaFile> MediaFiles { get; set; } = [];
    
    public ICollection<CatalogRecord>? CatalogRecords { get; set; } = [];
}