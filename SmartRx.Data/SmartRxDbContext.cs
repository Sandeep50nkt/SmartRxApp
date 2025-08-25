using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SmartRx.Domain;
using System.Text.Json;

namespace SmartRx.Data;

public class SmartRxDbContext : DbContext
{
    public SmartRxDbContext(DbContextOptions<SmartRxDbContext> options) : base(options) {}

    public DbSet<User> Users => Set<User>();
    public DbSet<Drug> Drugs => Set<Drug>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Username).IsUnique();
            e.Property(x => x.Username).IsRequired();
            e.Property(x => x.PasswordHash).IsRequired();
            e.Property(x => x.Salt).IsRequired();
            e.Property(x => x.Role).IsRequired();
        });

        modelBuilder.Entity<Drug>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.BrandName).IsRequired();
            e.Property(x => x.Manufacturer).IsRequired();

            e.Property(x => x.Ingredients)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, new JsonSerializerOptions()),
                    v => JsonSerializer.Deserialize<List<string>>(v, new JsonSerializerOptions()) ?? new List<string>())
                .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()));
        });
    }
}
