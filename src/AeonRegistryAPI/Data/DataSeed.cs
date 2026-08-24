using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using AeonRegistryAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AeonRegistryAPI.Data;

public class DataSeed
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task ManageDataAsync(IServiceProvider svcProvider)
    {
        await using var dbContextSvc = svcProvider.GetRequiredService<ApplicationDbContext>();

        // Apply any pending migrations
        await dbContextSvc.Database.MigrateAsync();

        // Identity-related seeds (roles, admin user, etc.)
        await SeedRolesAsync(svcProvider);
        await SeedUsersAsync(svcProvider);

        // Call seeders in order
        await SeedSitesAsync(dbContextSvc);
        await SeedArtifactsAsync(dbContextSvc);
        await SeedArtifactMediaFilesAsync(dbContextSvc);
        await SeedCatalogRecordsAsync(svcProvider);
        await ResetPostgresSequencesAsync(dbContextSvc);

    }

    #region seed data

    public static string GetSeedPath(params string[] paths)
    {
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "SeedData");
        return Path.Combine(basePath, Path.Combine(paths));
    }

    private static async Task SeedSitesAsync(ApplicationDbContext context)
    {
        if (await context.Sites.AnyAsync())
        {
            Console.WriteLine("Sites already exist. Skipping seeding.");
            return;
        }
        
        var filePath = GetSeedPath("sites.json");

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Sites Seed file not found: {filePath}");
            return; // skip seeding if file is missing
        }

        var json = await File.ReadAllTextAsync(filePath);
        var sites = JsonSerializer.Deserialize<List<Site>>(json, _jsonOptions);

        if (sites != null && sites.Count > 0)
        {
            context.Sites.AddRange(sites);
            await context.SaveChangesAsync();
            Console.WriteLine($"Seeded {sites.Count} sites.");
        }
    }

    // Seed base artifacts
    private static async Task SeedArtifactsAsync(ApplicationDbContext context)
    {
        if (await context.Artifacts.AnyAsync()) 
        {
            Console.WriteLine("Artifacts already exist. Skipping seeding.");
            return;
        }
       
        var filePath = GetSeedPath("artifacts.json");

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Artifact seed file not found: {filePath}");
            return;
        }

        var json = await File.ReadAllTextAsync(filePath);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(new JsonStringEnumConverter()); // <-- accept "Type": "EnergySource" etc.

        var artifacts = JsonSerializer.Deserialize<List<Artifact>>(json, _jsonOptions);

        if (artifacts != null && artifacts.Count > 0)
        {
            foreach (var artifact in artifacts)
            {
                artifact.DateDiscovered = DateTime.SpecifyKind(artifact.DateDiscovered, DateTimeKind.Utc);
            }

            context.Artifacts.AddRange(artifacts);
            await context.SaveChangesAsync();
            Console.WriteLine($"Seeded {artifacts.Count} artifacts.");
        }
    }

    // Seed sample catalog records
    public static async Task SeedCatalogRecordsAsync(IServiceProvider svcProvider)
    {
        var dbContext = svcProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = svcProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (await dbContext.CatalogRecords.AnyAsync())
        {
            Console.WriteLine("Catalog records already exist. Skipping seeding.");
            return;
        }

        // List of site-based JSON files
        var files = new[]
        {
            "catalogRecords.atlantis.json",
            "catalogRecords.sahara.json",
            "catalogRecords.andes.json",
            "catalogRecords.antarctica.json",
            "catalogRecords.gobekli.json",
            "catalogRecords.yonaguni.json",
            "catalogRecords.kailash.json"
        };

        foreach (var fileName in files)
        {
            var filePath = GetSeedPath(fileName);

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Seed file not found: {filePath}");
                continue;
            }

            var json = await File.ReadAllTextAsync(filePath);
            var records = JsonSerializer.Deserialize<List<CatalogRecordImport>>(json, _jsonOptions);

            if (records == null) continue;

            foreach (var recordImport in records)
            {
                // Find Artifact by CatalogNumber
                var artifact = await dbContext.Artifacts
                    .FirstOrDefaultAsync(a => a.CatalogNumber == recordImport.ArtifactCatalogNumber);

                if (artifact == null)
                {
                    Console.WriteLine($"Artifact not found: {recordImport.ArtifactCatalogNumber}");
                    continue;
                }

                recordImport.DateSubmitted = DateTime.SpecifyKind(recordImport.DateSubmitted, DateTimeKind.Utc);

                // Resolve SubmittedBy + VerifiedBy users
                var submittedBy = await userManager.FindByEmailAsync(recordImport.SubmittedBy);
                var verifiedBy = !string.IsNullOrEmpty(recordImport.VerifiedBy)
                    ? await userManager.FindByEmailAsync(recordImport.VerifiedBy)
                    : null;

                if (submittedBy == null)
                {
                    Console.WriteLine($"SubmittedBy user not found: {recordImport.SubmittedBy}");
                    continue;
                }

                var catalogRecord = new CatalogRecord
                {
                    ArtifactId = artifact.Id,
                    SubmittedById = submittedBy.Id,
                    VerifiedById = verifiedBy?.Id,
                    Status = recordImport.Status,
                    DateSubmitted = recordImport.DateSubmitted
                };

                // Add notes
                foreach (var noteImport in recordImport.Notes)
                {
                    var author = await userManager.FindByEmailAsync(noteImport.Author);
                    if (author == null)
                    {
                        Console.WriteLine($"Note Author not found: {noteImport.Author}");
                        continue;
                    }

                    noteImport.Created = DateTime.SpecifyKind(noteImport.Created, DateTimeKind.Utc);

                    catalogRecord.Notes ??= new List<CatalogNote>();
                    catalogRecord.Notes.Add(new CatalogNote
                    {
                        AuthorId = author.Id,
                        Content = noteImport.Content,
                        DateCreated = noteImport.Created
                    });
                }

                dbContext.CatalogRecords.Add(catalogRecord);
            }
        }

        await dbContext.SaveChangesAsync();
        Console.WriteLine("Catalog records seeded successfully.");
    }
    
    // Seed demo media (images)
    public static async Task SeedArtifactMediaFilesAsync(ApplicationDbContext context)
    {
        if (await context.ArtifactMediaFiles.AnyAsync())
        {
            Console.WriteLine("Artifact media already seeded. Skipping.");
            return;
        }

        var imagesPath = GetSeedPath("Images");
        if (!Directory.Exists(imagesPath))
        {
            Console.WriteLine("No image folder found: " + imagesPath);
            return;
        }

        // Only allow common image formats
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };

        foreach (var file in Directory.GetFiles(imagesPath))
        {
            var ext = Path.GetExtension(file).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
            {
                Console.WriteLine($"Skipping non-image file: {file}");
                continue;
            }

            // Example: "ATL-001-a.jpg" → "ATL-001"
            var baseName = Path.GetFileNameWithoutExtension(file);
            var parts = baseName.Split('-');
            string catalogNumber = parts.Length >= 2 ? $"{parts[0]}-{parts[1]}" : baseName;

            var artifact = await context.Artifacts
                .FirstOrDefaultAsync(a => a.CatalogNumber == catalogNumber);

            if (artifact == null)
            {
                Console.WriteLine($"No artifact found for {baseName}");
                continue;
            }

            var data = await File.ReadAllBytesAsync(file);
            var contentType = ext switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                _ => "application/octet-stream"
            };

            context.ArtifactMediaFiles.Add(new ArtifactMediaFile
            {
                ArtifactId = artifact.Id,
                FileName = Path.GetFileName(file),
                ContentType = contentType,
                Data = data,
                IsPrimary = true // mark first image as default
            });

            Console.WriteLine($"Seeded {Path.GetFileName(file)} for artifact {catalogNumber}");
        }

        await context.SaveChangesAsync();
        Console.WriteLine("Artifact images seeded successfully.");
    }

    private static async Task ResetPostgresSequencesAsync(ApplicationDbContext context)
    {
        // Reset sequence for Sites table
        await context.Database.ExecuteSqlRawAsync(@"
     SELECT setval(
         pg_get_serial_sequence('""Sites""', 'Id'),
         COALESCE(MAX(""Id""), 1)
     ) FROM ""Sites"";");

        // Optionally repeat for other tables that use explicit IDs in seed data:
        await context.Database.ExecuteSqlRawAsync(@"
     SELECT setval(
         pg_get_serial_sequence('""Artifacts""', 'Id'),
         COALESCE(MAX(""Id""), 1)
     ) FROM ""Artifacts"";");

        await context.Database.ExecuteSqlRawAsync(@"
     SELECT setval(
         pg_get_serial_sequence('""CatalogRecords""', 'Id'),
         COALESCE(MAX(""Id""), 1)
     ) FROM ""CatalogRecords"";");

        Console.WriteLine("✅ PostgreSQL identity sequences reset successfully.");
    }

    // DTOs for import
    private class CatalogRecordImport
    {
        public string ArtifactCatalogNumber { get; set; } = string.Empty;
        public string SubmittedBy { get; set; } = string.Empty;
        public string? VerifiedBy { get; set; }
        public string Status { get; set; } = "Draft";
        public DateTime DateSubmitted { get; set; }
        public List<CatalogNoteImport> Notes { get; set; } = [];
    }

    private class CatalogNoteImport
    {
        public string Author { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime Created { get; set; }
    }

    #endregion seed data

    #region seed roles and users

    private static async Task SeedRolesAsync(IServiceProvider svcProvider)
    {
        var roleManager = svcProvider.GetRequiredService<RoleManager<IdentityRole>>();

        string[] roles = ["Admin", "Archivist", "Researcher", "Viewer"];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private static async Task SeedUsersAsync(IServiceProvider svcProvider)
    {
        var userManager = svcProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Admin
        var admin = await GetOrCreateUserAsync(userManager, "Admin", "User", "admin@aeon.org", "Admin123!", "Admin");
        if (admin != null)
        {
            await AddClaimIfNotExists(userManager, admin, "CanVerifyCatalogRecords", "true");
            await AddClaimIfNotExists(userManager, admin, "CanUploadMedia", "true");
            await AddClaimIfNotExists(userManager, admin, "CanManageUsers", "true");
        }

        // Archivist
        var archivist = await GetOrCreateUserAsync(userManager, "Archivist", "User", "archivist@aeon.org", "Archivist123!", "Archivist");
        if (archivist != null)
        {
            await AddClaimIfNotExists(userManager, archivist, "CanVerifyCatalogRecords", "true");
        }

        // Researcher
        var researcher = await GetOrCreateUserAsync(userManager, "Researcher", "User", "researcher@aeon.org", "Researcher123!", "Researcher");
        if (researcher != null)
        {
            await AddClaimIfNotExists(userManager, researcher, "CanUploadMedia", "true");
        }

        // Viewer
        await GetOrCreateUserAsync(userManager, "Viewer", "User", "viewer@aeon.org", "Viewer123!", "Viewer");
    }

    private static async Task<ApplicationUser?> GetOrCreateUserAsync(
        UserManager<ApplicationUser> userManager,
        string firstName,
        string lastName,
        string email,
        string password,
        string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = firstName,
                LastName = lastName
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded) return null;
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }

        return user;
    }

    private static async Task AddClaimIfNotExists(UserManager<ApplicationUser> userManager, ApplicationUser user, string claimType, string claimValue)
    {
        var claims = await userManager.GetClaimsAsync(user);
        if (!claims.Any(c => c.Type == claimType && c.Value == claimValue))
        {
            await userManager.AddClaimAsync(user, new Claim(claimType, claimValue));
        }
    }

    #endregion seed roles and users
}