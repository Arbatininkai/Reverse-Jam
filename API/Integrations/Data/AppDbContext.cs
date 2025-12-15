using Microsoft.EntityFrameworkCore;
namespace Integrations.Data.Entities;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<LobbyEntity> Lobbies => Set<LobbyEntity>();
    public DbSet<RecordingEntity> Recordings => Set<RecordingEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<LobbyEntity>().ToTable("lobby");
        modelBuilder.Entity<UserEntity>().ToTable("users");
        modelBuilder.Entity<RecordingEntity>().ToTable("Recording");

        // Many-to-many: User <-> Lobby
        modelBuilder.Entity<LobbyEntity>()
            .HasMany(l => l.Players)
            .WithMany(u => u.Lobbies)
            .UsingEntity<Dictionary<string, object>>(
                "LobbyPlayers",
                j => j.HasOne<UserEntity>().WithMany().HasForeignKey("PlayerId"),
                j => j.HasOne<LobbyEntity>().WithMany().HasForeignKey("LobbyId"),
                j => j.HasKey("LobbyId", "PlayerId")
            );


        // One-to-many: Lobby -> Recordings
        modelBuilder.Entity<LobbyEntity>()
            .HasMany(l => l.Recordings)
            .WithOne(r => r.Lobby)
            .HasForeignKey(r => r.LobbyId)
            .OnDelete(DeleteBehavior.Cascade);

        // One-to-many: User -> Recordings
        modelBuilder.Entity<UserEntity>()
            .HasMany(u => u.Recordings)
            .WithOne(r => r.User)
            .HasForeignKey(r => r.UserId);

        // One-to-many: Lobby -> Owner (User)
        /*modelBuilder.Entity<LobbyEntity>()
            .HasOne(l => l.Owner)
            .WithMany() // no navigation property on UserEntity for owned lobbies
            .HasForeignKey(l => l.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);*/
    }
}