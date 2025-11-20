using Microsoft.EntityFrameworkCore;
using API.Models;

namespace API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<User> Users { get; set; }
    public DbSet<Lobby> Lobbies { get; set; }
    public DbSet<Recording> Recordings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Lobby>().ToTable("lobby");
        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<Recording>().ToTable("Recording");

        // Many-to-many: User <-> Lobby
        modelBuilder.Entity<Lobby>()
            .HasMany(l => l.Players)
            .WithMany(u => u.Lobbies)
            .UsingEntity<Dictionary<string, object>>(
                "LobbyPlayers",
                j => j.HasOne<User>().WithMany().HasForeignKey("PlayerId"),
                j => j.HasOne<Lobby>().WithMany().HasForeignKey("LobbyId"),
                j => j.HasKey("LobbyId", "PlayerId")
            );


        // One-to-many: Lobby -> Recordings
        modelBuilder.Entity<Lobby>()
            .HasMany(l => l.Recordings)
            .WithOne(r => r.Lobby)
            .HasForeignKey(r => r.LobbyId)
            .OnDelete(DeleteBehavior.Cascade);

        // One-to-many: User -> Recordings
        modelBuilder.Entity<User>()
            .HasMany(u => u.Recordings)
            .WithOne(r => r.User)
            .HasForeignKey(r => r.UserId);
    }
}