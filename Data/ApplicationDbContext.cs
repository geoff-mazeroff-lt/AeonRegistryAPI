using AeonRegistryAPI.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AeonRegistryAPI.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Artifact> Artifacts { get; set; }
    public DbSet<Site> Sites { get; set; }
    public DbSet<ArtifactMediaFile> ArtifactMediaFiles { get; set; }
    public DbSet<CatalogNote> CatalogNotes { get; set; }
    public DbSet<CatalogRecord> CatalogRecords { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        builder.Entity<CatalogRecord>()
            .HasOne(cr => cr.SubmittedBy)
            .WithMany(u => u.SubmittedCatalogRecords)
            .HasForeignKey(cr => cr.SubmittedById)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.Entity<CatalogRecord>()
            .HasOne(cr => cr.VerifiedBy)
            .WithMany(u => u.VerifiedCatalogRecords)
            .HasForeignKey(cr => cr.VerifiedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Artifact>()
            .Property(a => a.Type)
            .HasConversion<string>();
        
        builder.Entity<CatalogRecord>()
            .Property(a => a.Status)
            .HasConversion<string>();
    }
}