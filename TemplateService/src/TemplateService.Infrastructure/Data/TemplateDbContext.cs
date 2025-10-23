using Microsoft.EntityFrameworkCore;
using TemplateService.Core.Models;

namespace TemplateService.Infrastructure.Data;

/// <summary>
/// Database context for the Template Service.
/// </summary>
public class TemplateDbContext : DbContext
{
    public TemplateDbContext(DbContextOptions<TemplateDbContext> options) : base(options)
    {
    }

    public DbSet<Template> Templates => Set<Template>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<TemplateTag> TemplateTags => Set<TemplateTag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Template configuration
        modelBuilder.Entity<Template>(entity =>
        {
            entity.ToTable("templates");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.TopologyJson).IsRequired();
            entity.Property(e => e.ThumbnailUrl).HasMaxLength(500);
            entity.Property(e => e.Version).HasDefaultValue(1);
            entity.Property(e => e.IsPublic).HasDefaultValue(false);
            entity.Property(e => e.UsageCount).HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Self-referencing relationship for cloning
            entity.HasOne(e => e.ClonedFrom)
                  .WithMany()
                  .HasForeignKey(e => e.ClonedFromId)
                  .OnDelete(DeleteBehavior.SetNull);

            // Indexes
            entity.HasIndex(e => e.OwnerId);
            entity.HasIndex(e => e.IsPublic);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Name);
        });

        // Tag configuration
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.ToTable("tags");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.ColorCode).HasMaxLength(7);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Unique constraint on tag name
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // TemplateTag configuration (many-to-many)
        modelBuilder.Entity<TemplateTag>(entity =>
        {
            entity.ToTable("template_tags");
            entity.HasKey(e => new { e.TemplateId, e.TagId });

            entity.HasOne(e => e.Template)
                  .WithMany(t => t.Tags)
                  .HasForeignKey(e => e.TemplateId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Tag)
                  .WithMany(t => t.TemplateTags)
                  .HasForeignKey(e => e.TagId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Seed some default tags
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        var now = DateTime.UtcNow;

        var defaultTags = new[]
        {
            new Tag { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "wifi", Description = "WiFi network templates", ColorCode = "#3B82F6", CreatedAt = now },
            new Tag { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "p2p", Description = "Point-to-point network templates", ColorCode = "#10B981", CreatedAt = now },
            new Tag { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "csma", Description = "CSMA/CD network templates", ColorCode = "#F59E0B", CreatedAt = now },
            new Tag { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), Name = "lte", Description = "LTE network templates", ColorCode = "#8B5CF6", CreatedAt = now },
            new Tag { Id = Guid.Parse("55555555-5555-5555-5555-555555555555"), Name = "mesh", Description = "Mesh network templates", ColorCode = "#EC4899", CreatedAt = now },
        };

        modelBuilder.Entity<Tag>().HasData(defaultTags);
    }
}

