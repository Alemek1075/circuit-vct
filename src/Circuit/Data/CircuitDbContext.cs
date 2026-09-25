using Circuit.Models;
using Microsoft.EntityFrameworkCore;

namespace Circuit.Data;

public sealed class CircuitDbContext(DbContextOptions<CircuitDbContext> options) : DbContext(options)
{
    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Match> Matches => Set<Match>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tournament>().HasIndex(t => t.Slug).IsUnique();
        modelBuilder.Entity<Team>().HasIndex(t => new { t.TournamentId, t.ShortName }).IsUnique();
        modelBuilder.Entity<Match>().HasIndex(m => new { m.TournamentId, m.Code }).IsUnique();
        modelBuilder.Entity<Match>().HasOne(m => m.TeamA).WithMany().HasForeignKey(m => m.TeamAId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Match>().HasOne(m => m.TeamB).WithMany().HasForeignKey(m => m.TeamBId).OnDelete(DeleteBehavior.Restrict);
    }
}
