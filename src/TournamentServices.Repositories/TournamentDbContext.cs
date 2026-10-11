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
    internal DbSet<GroupTeam> GroupTeams => Set<GroupTeam>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureTeam(modelBuilder);
        ConfigureGroup(modelBuilder);
        ConfigureMatch(modelBuilder);
        ConfigureTournament(modelBuilder);
        ConfigureGroupTeam(modelBuilder);

        ConfigureSoftDelete<Team>(modelBuilder);
        ConfigureSoftDelete<Group>(modelBuilder);
        ConfigureSoftDelete<Match>(modelBuilder);
        ConfigureSoftDelete<Tournament>(modelBuilder);
    }

    private static void ConfigureTeam(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Team>(b =>
        {
            b.ToTable("teams");
            b.HasKey(t => t.Id);
            b.Property(t => t.Id).HasColumnName("id");
            b.Property(t => t.Name).HasColumnName("name").IsRequired();
            b.HasIndex(t => t.Name).IsUnique().HasFilter("deleted_at IS NULL");
        });
    }

       private static void ConfigureTournament(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tournament>(b =>
        {
            b.ToTable("tournaments");
            b.HasKey(t => t.Id);

            b.Property(t => t.Id).HasColumnName("id");
            b.Property(t => t.Name).HasColumnName("name").IsRequired();
            b.HasIndex(t => t.Name).IsUnique().HasFilter("deleted_at IS NULL");

            b.OwnsOne(t => t.Format, f =>
            {
                f.Property(x => x.MaxTeamsPerGroup).HasColumnName("max_teams_per_group");
                f.Property(x => x.NumberOfGroups).HasColumnName("number_of_groups");
                f.Property(x => x.Type).HasColumnName("format_type").HasConversion<string>();
            });
            b.Navigation(t => t.Format).IsRequired();
            
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

    private static void ConfigureGroup(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Group>(b =>
        {
            b.ToTable("groups");
            b.HasKey(g => g.Id);
            b.Property(g => g.Id).HasColumnName("id");
            b.Property(g => g.Name).HasColumnName("name").IsRequired();
            b.Property(g => g.TournamentId).HasColumnName("tournament_id").IsRequired();
            b.HasIndex(g => new { g.TournamentId, g.Name }).IsUnique().HasFilter("deleted_at IS NULL");

            b.Ignore(g => g.TeamIds);
        });
    }

    private static void ConfigureMatch(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Match>(b =>
        {
            b.ToTable("matches");
            b.HasKey(m => m.Id);
            b.Property(m => m.Id).HasColumnName("id");
            b.Property(m => m.TournamentId).HasColumnName("tournament_id").IsRequired();
            b.Property(m => m.GroupId).HasColumnName("group_id");

            b.Property(m => m.HomeTeamId)
                .HasColumnName("home_team_id")
                .IsRequired()
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            b.Property(m => m.VisitorTeamId)
                .HasColumnName("visitor_team_id")
                .IsRequired()
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            b.OwnsOne(m => m.Score, score =>
            {
                score.Property(s => s.HomeTeamScore).HasColumnName("home_team_score");
                score.Property(s => s.VisitorTeamScore).HasColumnName("visitor_team_score");
            });

            b.Property(m => m.Winner).HasColumnName("winner").HasConversion<string>();
            b.Property(m => m.IsCompleted).HasColumnName("is_completed");

            b.HasOne<Group>()
                .WithMany()
                .HasForeignKey(m => m.GroupId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureGroupTeam(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GroupTeam>(b =>
        {
            b.ToTable("group_teams");
            b.HasKey(gt => new { gt.GroupId, gt.TeamId });
            b.Property(gt => gt.GroupId).HasColumnName("group_id");
            b.Property(gt => gt.TeamId).HasColumnName("team_id");
        });
    }

    private static void ConfigureSoftDelete<T>(ModelBuilder modelBuilder) where T : class
    {
        modelBuilder.Entity<T>().Property<DateTime?>("deleted_at");
        modelBuilder.Entity<T>().HasQueryFilter(e => EF.Property<DateTime?>(e, "deleted_at") == null);
    }
}