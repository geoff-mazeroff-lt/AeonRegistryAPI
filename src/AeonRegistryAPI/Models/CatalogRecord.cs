using System.ComponentModel.DataAnnotations;
using AeonRegistryAPI.Enums;

namespace AeonRegistryAPI.Models;

public class CatalogRecord
{
    public int Id { get; set; }
    
    [Required]
    public int ArtifactId { get; set; }
    public Artifact? Artifact { get; set; }
    
    [Required]
    public string SubmittedById { get; set; } = string.Empty;
    public ApplicationUser? SubmittedBy { get; set; }
    
    public string? VerifiedById { get; set; } = string.Empty;
    public ApplicationUser? VerifiedBy { get; set; }

    [Required]
    public string Status { get; set; } = nameof(CatalogStatus.Draft);
    
    [Required]
    public DateTime DateSubmitted { get; set; } = DateTime.UtcNow;

    public ICollection<CatalogNote>? Notes { get; set; } = [];
}