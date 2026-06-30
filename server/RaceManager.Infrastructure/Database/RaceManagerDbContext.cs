using Microsoft.EntityFrameworkCore;
using RaceManager.Infrastructure.Database.Models;

namespace RaceManager.Infrastructure.Database;

public sealed class RaceManagerDbContext(DbContextOptions<RaceManagerDbContext> options) : DbContext(options)
{
    public DbSet<RoleEntity> Roles => Set<RoleEntity>();
    public DbSet<DisciplineEntity> Disciplines => Set<DisciplineEntity>();
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<TeamEntity> Teams => Set<TeamEntity>();
    public DbSet<DriverEntity> Drivers => Set<DriverEntity>();
    public DbSet<CarEntity> Cars => Set<CarEntity>();
    public DbSet<TrackEntity> Tracks => Set<TrackEntity>();
    public DbSet<ChampionshipEntity> Championships => Set<ChampionshipEntity>();
    public DbSet<EventEntity> Events => Set<EventEntity>();
    public DbSet<RegistrationEntity> Registrations => Set<RegistrationEntity>();
    public DbSet<EventJudgeEntity> EventJudges => Set<EventJudgeEntity>();
    public DbSet<StartListEntity> StartLists => Set<StartListEntity>();
    public DbSet<ResultEntity> Results => Set<ResultEntity>();
    public DbSet<PenaltyEntity> Penalties => Set<PenaltyEntity>();
    public DbSet<ChampionshipStandingEntity> ChampionshipStandings => Set<ChampionshipStandingEntity>();
    public DbSet<TeamStandingEntity> TeamStandings => Set<TeamStandingEntity>();
    public DbSet<MediaItemEntity> MediaItems => Set<MediaItemEntity>();
    public DbSet<SupportTicketEntity> SupportTickets => Set<SupportTicketEntity>();
    public DbSet<SupportMessageEntity> SupportMessages => Set<SupportMessageEntity>();
    public DbSet<PointsRuleEntity> PointsRules => Set<PointsRuleEntity>();
    public DbSet<ClassRuleEntity> ClassRules => Set<ClassRuleEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RoleEntity>(e =>
        {
            e.ToTable("Roles");
            e.HasKey(x => x.RoleId);
            e.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<DisciplineEntity>(e =>
        {
            e.ToTable("Disciplines");
            e.HasKey(x => x.DisciplineId);
            e.HasIndex(x => x.Name).IsUnique();
        });



        modelBuilder.Entity<ClassRuleEntity>(e =>
        {
            e.ToTable("ClassRules");
            e.HasKey(x => x.ClassRuleId);
            e.Property(x => x.MinTimeSeconds).HasPrecision(8, 3);
            e.Property(x => x.MaxTimeSeconds).HasPrecision(8, 3);
            e.HasIndex(x => new { x.DisciplineId, x.Mode, x.MinTimeSeconds, x.MaxTimeSeconds });
            e.HasOne(x => x.Championship).WithMany().HasForeignKey(x => x.ChampionshipId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Discipline).WithMany(x => x.ClassRules).HasForeignKey(x => x.DisciplineId);
        });

        modelBuilder.Entity<UserEntity>(e =>
        {
            e.ToTable("Users");
            e.HasKey(x => x.UserId);
            e.HasIndex(x => x.Login).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.ExternalId).IsUnique().HasFilter("[ExternalId] IS NOT NULL");
            e.HasOne(x => x.Role).WithMany(x => x.Users).HasForeignKey(x => x.RoleId);
        });

        modelBuilder.Entity<TeamEntity>(e =>
        {
            e.ToTable("Teams");
            e.HasKey(x => x.TeamId);
            e.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<DriverEntity>(e =>
        {
            e.ToTable("Drivers");
            e.HasKey(x => x.DriverId);
            e.Property(x => x.TotalPoints).HasPrecision(10, 2);
            e.HasIndex(x => x.UserId).IsUnique();
            e.HasOne(x => x.User).WithOne(x => x.Driver).HasForeignKey<DriverEntity>(x => x.UserId);
            e.HasOne(x => x.Team).WithMany(x => x.Drivers).HasForeignKey(x => x.TeamId);
        });

        modelBuilder.Entity<CarEntity>(e =>
        {
            e.ToTable("Cars");
            e.HasKey(x => x.CarId);
            e.Property(x => x.ImageUrl).HasColumnType("nvarchar(max)");
            e.Property(x => x.PowerToWeight).HasPrecision(10, 2);
            e.HasIndex(x => x.ExternalId).IsUnique().HasFilter("[ExternalId] IS NOT NULL");
            e.HasOne(x => x.User).WithMany(x => x.Cars).HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<TrackEntity>(e =>
        {
            e.ToTable("Tracks");
            e.HasKey(x => x.TrackId);
        });

        modelBuilder.Entity<ChampionshipEntity>(e =>
        {
            e.ToTable("Championships");
            e.HasKey(x => x.ChampionshipId);
            e.Property(x => x.BannerUrl).HasColumnType("nvarchar(max)");
            e.HasOne(x => x.Organizer).WithMany(x => x.OrganizedChampionships).HasForeignKey(x => x.OrganizerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Discipline).WithMany(x => x.Championships).HasForeignKey(x => x.DisciplineId);
        });

        modelBuilder.Entity<EventEntity>(e =>
        {
            e.ToTable("Events");
            e.HasKey(x => x.EventId);
            e.Property(x => x.BannerUrl).HasColumnType("nvarchar(max)");
            e.Property(x => x.TrackConfigImageUrl).HasColumnType("nvarchar(max)");
            e.Property(x => x.CalendarBannerUrl).HasColumnType("nvarchar(max)");
            e.Property(x => x.OrganizerLogoUrl).HasColumnType("nvarchar(max)");
            e.Property(x => x.StagesJson).HasColumnType("nvarchar(max)");
            e.HasIndex(x => x.ExternalId).IsUnique().HasFilter("[ExternalId] IS NOT NULL");
            e.HasOne(x => x.Championship).WithMany(x => x.Events).HasForeignKey(x => x.ChampionshipId);
            e.HasOne(x => x.Organizer).WithMany(x => x.OrganizedEvents).HasForeignKey(x => x.OrganizerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Track).WithMany(x => x.Events).HasForeignKey(x => x.TrackId);
            e.HasOne(x => x.Discipline).WithMany(x => x.Events).HasForeignKey(x => x.DisciplineId);
        });

        modelBuilder.Entity<RegistrationEntity>(e =>
        {
            e.ToTable("Registrations");
            e.HasKey(x => x.RegistrationId);
            e.HasIndex(x => new { x.EventId, x.UserId }).IsUnique();
            e.Property(x => x.QualificationTimeSeconds).HasPrecision(8, 3);
            e.HasOne(x => x.Event).WithMany(x => x.Registrations).HasForeignKey(x => x.EventId);
            e.HasOne(x => x.User).WithMany(x => x.Registrations).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Car).WithMany().HasForeignKey(x => x.CarId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EventJudgeEntity>(e =>
        {
            e.ToTable("EventJudges");
            e.HasKey(x => x.EventJudgeId);
            e.HasIndex(x => new { x.EventId, x.JudgeUserId }).IsUnique();
            e.HasOne(x => x.Event).WithMany(x => x.EventJudges).HasForeignKey(x => x.EventId);
            e.HasOne(x => x.JudgeUser).WithMany().HasForeignKey(x => x.JudgeUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StartListEntity>(e =>
        {
            e.ToTable("StartLists");
            e.HasKey(x => x.StartListId);
            e.HasIndex(x => x.RegistrationId).IsUnique();
            e.HasOne(x => x.Event).WithMany().HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Registration).WithOne(x => x.StartList).HasForeignKey<StartListEntity>(x => x.RegistrationId);
        });

        modelBuilder.Entity<ResultEntity>(e =>
        {
            e.ToTable("Results");
            e.HasKey(x => x.ResultId);
            e.HasIndex(x => x.ExternalId).IsUnique().HasFilter("[ExternalId] IS NOT NULL");
            e.Property(x => x.Points).HasPrecision(10, 2);
            e.HasIndex(x => x.RegistrationId).IsUnique();
            e.HasOne(x => x.Event).WithMany().HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Registration).WithOne(x => x.Result).HasForeignKey<ResultEntity>(x => x.RegistrationId);
        });

        modelBuilder.Entity<PenaltyEntity>(e =>
        {
            e.ToTable("Penalties");
            e.HasKey(x => x.PenaltyId);
            e.Property(x => x.TimeSeconds).HasPrecision(10, 2);
            e.Property(x => x.Points).HasPrecision(10, 2);
            e.HasOne(x => x.Result).WithMany(x => x.Penalties).HasForeignKey(x => x.ResultId);
            e.HasOne(x => x.JudgeUser).WithMany().HasForeignKey(x => x.JudgeUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ChampionshipStandingEntity>(e =>
        {
            e.ToTable("ChampionshipStandings");
            e.HasKey(x => x.ChampionshipStandingId);
            e.Property(x => x.TotalPoints).HasPrecision(10, 2);
            e.HasIndex(x => new { x.ChampionshipId, x.DriverId }).IsUnique();
            e.HasIndex(x => new { x.ChampionshipId, x.Position }).IsUnique();
        });

        modelBuilder.Entity<TeamStandingEntity>(e =>
        {
            e.ToTable("TeamStandings");
            e.HasKey(x => x.TeamStandingId);
            e.Property(x => x.TotalPoints).HasPrecision(10, 2);
            e.HasIndex(x => new { x.ChampionshipId, x.TeamId }).IsUnique();
            e.HasIndex(x => new { x.ChampionshipId, x.Position }).IsUnique();
        });

        modelBuilder.Entity<MediaItemEntity>(e =>
        {
            e.ToTable("MediaItems");
            e.HasKey(x => x.MediaItemId);
        });

        modelBuilder.Entity<SupportTicketEntity>(e =>
        {
            e.ToTable("SupportTickets");
            e.HasKey(x => x.SupportTicketId);
            e.HasIndex(x => x.ExternalId).IsUnique().HasFilter("[ExternalId] IS NOT NULL");
        });

        modelBuilder.Entity<SupportMessageEntity>(e =>
        {
            e.ToTable("SupportMessages");
            e.HasKey(x => x.SupportMessageId);
            e.HasIndex(x => x.ExternalId).IsUnique().HasFilter("[ExternalId] IS NOT NULL");
            e.HasOne(x => x.Ticket).WithMany(x => x.Messages).HasForeignKey(x => x.SupportTicketId);
        });

        modelBuilder.Entity<PointsRuleEntity>(e =>
        {
            e.ToTable("PointsRules");
            e.HasKey(x => x.PointsRuleId);
            e.Property(x => x.Points).HasPrecision(10, 2);
            e.HasIndex(x => new { x.Name, x.Position }).IsUnique();
        });
    }
}
