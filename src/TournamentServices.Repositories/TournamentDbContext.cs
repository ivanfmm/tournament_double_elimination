using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TournamentServices.Domain;

namespace TournamentServices.Repositories;

public class TournamentDbContext : DbContext
{
    public TournamentDbContext(DbContextOptions<TournamentDbContext> options) : base(options) { }

    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<Tournament> Tournaments => Set<Tournament>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureTeam(modelBuilder);
        ConfigureGroup(modelBuilder);
        ConfigureMatch(modelBuilder);
        ConfigureTournament(modelBuilder);
    }

    private static void ConfigureTeam(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Team>(b =>
        {
            b.ToTable("Teams");
            b.HasKey(t => t.Id);
            b.Property(t => t.Name).IsRequired();
        });
    }

    private static void ConfigureGroup(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Group>(b =>
        {
            b.ToTable("Groups");
            b.HasKey(g => g.Id);
            b.Property(g => g.Name).IsRequired();
            b.Property(g => g.TournamentId).IsRequired();

            b.Property<List<string>>("_teamIds")
                .HasColumnName("TeamIds")
                .HasConversion(
                    teamIds => JsonSerializer.Serialize(teamIds, (JsonSerializerOptions?)null),
                    json => JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null)
                            ?? new List<string>())
                .Metadata.SetPropertyAccessMode(PropertyAccessMode.Field);
        });
    }

    private static void ConfigureMatch(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Match>(b =>
        {
            b.ToTable("Matches");
            b.HasKey(m => m.Id);
            b.Property(m => m.TournamentId).IsRequired();
            b.Property(m => m.GroupId);

            b.Property(m => m.HomeTeamId)
                .HasColumnName("HomeTeamId")
                .IsRequired()
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            b.Property(m => m.VisitorTeamId)
                .HasColumnName("VisitorTeamId")
                .IsRequired()
                .UsePropertyAccessMode(PropertyAccessMode.Field);
            // Score es un value object embebido (owned type): sus dos
            // enteros se guardan como columnas normales DENTRO de la
            // misma tabla Matches, no en una tabla separada.
            b.OwnsOne(m => m.Score, score =>
            {
                score.Property(s => s.HomeTeamScore).HasColumnName("HomeTeamScore");
                score.Property(s => s.VisitorTeamScore).HasColumnName("VisitorTeamScore");
            });

            b.Property(m => m.Winner).HasConversion<string>();
            b.Property(m => m.IsCompleted);

            // GroupId es opcional: si se borra el Group, el match NO se
            // borra, solo pierde la referencia (a diferencia de borrar
            // el Tournament completo, que sí hace cascada — ver abajo).
            b.HasOne<Group>()
                .WithMany()
                .HasForeignKey(m => m.GroupId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureTournament(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tournament>(b =>
        {
            b.ToTable("Tournaments");
            b.HasKey(t => t.Id);
            b.Property(t => t.Name).IsRequired();

            b.OwnsOne(t => t.Format, format =>
            {
                format.Property(f => f.MaxTeamsPerGroup).HasColumnName("MaxTeamsPerGroup");
                format.Property(f => f.NumberOfGroups).HasColumnName("NumberOfGroups");
                format.Property(f => f.Type).HasColumnName("FormatType").HasConversion<string>();
            });

            b.Ignore(t => t.Groups);
            b.Ignore(t => t.Matches);

            b.HasMany<Group>()
                .WithOne()
                .HasForeignKey(g => g.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasMany<Match>()
                .WithOne()
                .HasForeignKey(m => m.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}