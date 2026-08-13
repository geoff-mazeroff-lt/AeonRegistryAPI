using System.ComponentModel.DataAnnotations;

namespace AeonRegistryAPI.Models;

public class CatalogNote
{
    public int Id { get; set; }
    
    [Required]
    public int CatalogRecordId { get; set; }
    public CatalogRecord? CatalogRecord { get; set; }

    public string AuthorId { get; set; } = string.Empty;
    public ApplicationUser? Author { get; set; }
    
    [Required, MaxLength(1000)]
    public string Content { get; set; } = string.Empty;

    [Required]
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
}