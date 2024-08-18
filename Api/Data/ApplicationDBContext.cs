using DigipetApi.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DigipetApi.Api.Data;

public class ApplicationDBContext : DbContext
{
    public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        PetSeeder.SeedPets(modelBuilder);
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Pet> Pets { get; set; } = null!;
    public DbSet<ScheduledTask> ScheduledTasks { get; set; } = null!;
}