namespace RaceManager.Infrastructure.Database.Models;

public sealed class RoleEntity
{
    public int RoleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public ICollection<UserEntity> Users { get; set; } = new List<UserEntity>();
}

public sealed class DisciplineEntity
{
    public int DisciplineId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AccentColor { get; set; }
    public ICollection<ChampionshipEntity> Championships { get; set; } = new List<ChampionshipEntity>();
    public ICollection<EventEntity> Events { get; set; } = new List<EventEntity>();
    public ICollection<ClassRuleEntity> ClassRules { get; set; } = new List<ClassRuleEntity>();
}

public sealed class UserEntity
{
    public int UserId { get; set; }
    public string? ExternalId { get; set; }
    public int RoleId { get; set; }
    public string Login { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public string? OrganizationName { get; set; }
    public string? OrganizationColor { get; set; }
    public string? OrganizationLogoUrl { get; set; }
    public string? OrganizationBannerUrl { get; set; }
    public string? OrganizationMembersJson { get; set; }
    public string? RacingTeamName { get; set; }
    public string? RacingTeamColor { get; set; }
    public string? RacingTeamLogoUrl { get; set; }
    public string? RacingTeamBannerUrl { get; set; }
    public string? RacingTeamMembersJson { get; set; }
    public string? TeamInvitationsJson { get; set; }
    public string? TeamNotificationsJson { get; set; }
    public string? TeamMembershipsJson { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; }
    public RoleEntity? Role { get; set; }
    public DriverEntity? Driver { get; set; }
    public ICollection<CarEntity> Cars { get; set; } = new List<CarEntity>();
    public ICollection<ChampionshipEntity> OrganizedChampionships { get; set; } = new List<ChampionshipEntity>();
    public ICollection<EventEntity> OrganizedEvents { get; set; } = new List<EventEntity>();
    public ICollection<RegistrationEntity> Registrations { get; set; } = new List<RegistrationEntity>();
}

public sealed class TeamEntity
{
    public int TeamId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? Description { get; set; }
    public string? SocialUrl { get; set; }
    public string? AccentColor { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<DriverEntity> Drivers { get; set; } = new List<DriverEntity>();
}

public sealed class DriverEntity
{
    public int DriverId { get; set; }
    public int UserId { get; set; }
    public int? TeamId { get; set; }
    public string? LicenseNumber { get; set; }
    public string? DriverNumber { get; set; }
    public int? RatingPosition { get; set; }
    public decimal TotalPoints { get; set; }
    public int RacesCount { get; set; }
    public int WinsCount { get; set; }
    public int PodiumsCount { get; set; }
    public string? Bio { get; set; }
    public UserEntity? User { get; set; }
    public TeamEntity? Team { get; set; }
}

public sealed class CarEntity
{
    public int CarId { get; set; }
    public string? ExternalId { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? Type { get; set; }
    public int? PowerHp { get; set; }
    public int? WeightKg { get; set; }
    public decimal? PowerToWeight { get; set; }
    public string? DriveType { get; set; }
    public string? EngineType { get; set; }
    public string? EngineModel { get; set; }
    public int? EngineVolumeCm3 { get; set; }
    public int? TorqueNm { get; set; }
    public bool IsFavorite { get; set; }
    public bool IsTeamVehicle { get; set; }
    public string? TeamName { get; set; }
    public string? TeamLogoUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public UserEntity? User { get; set; }
}

public sealed class TrackEntity
{
    public int TrackId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? ConfigurationName { get; set; }
    public int? LengthMeters { get; set; }
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
    public ICollection<EventEntity> Events { get; set; } = new List<EventEntity>();
}

public sealed class ChampionshipEntity
{
    public int ChampionshipId { get; set; }
    public int OrganizerId { get; set; }
    public int DisciplineId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SeasonYear { get; set; }
    public string? BannerUrl { get; set; }
    public string? RegulationFileUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public UserEntity? Organizer { get; set; }
    public DisciplineEntity? Discipline { get; set; }
    public ICollection<EventEntity> Events { get; set; } = new List<EventEntity>();
}

public sealed class EventEntity
{
    public int EventId { get; set; }
    public string? ExternalId { get; set; }
    public int? ChampionshipId { get; set; }
    public int OrganizerId { get; set; }
    public int? TrackId { get; set; }
    public int DisciplineId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? ClassMode { get; set; }
    public int? StageNumber { get; set; }
    public string? Description { get; set; }
    public DateTime DateStart { get; set; }
    public DateTime? DateEnd { get; set; }
    public string RegistrationStatus { get; set; } = string.Empty;
    public int? MaxParticipants { get; set; }
    public int? LapsCount { get; set; }
    public int? DistanceMeters { get; set; }
    public string? TrackConfigImageUrl { get; set; }
    public string? BannerUrl { get; set; }
    public string? CalendarBannerUrl { get; set; }
    public string? OrganizerName { get; set; }
    public string? OrganizerColor { get; set; }
    public string? OrganizerLogoUrl { get; set; }
    public string? StagesJson { get; set; }
    public string? RegulationFileUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public ChampionshipEntity? Championship { get; set; }
    public UserEntity? Organizer { get; set; }
    public TrackEntity? Track { get; set; }
    public DisciplineEntity? Discipline { get; set; }
    public ICollection<RegistrationEntity> Registrations { get; set; } = new List<RegistrationEntity>();
    public ICollection<EventJudgeEntity> EventJudges { get; set; } = new List<EventJudgeEntity>();
}

public sealed class RegistrationEntity
{
    public int RegistrationId { get; set; }
    public int EventId { get; set; }
    public int UserId { get; set; }
    public int? CarId { get; set; }
    public string? ManualCarName { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? DriverNumber { get; set; }
    public decimal? QualificationTimeSeconds { get; set; }
    public string? ClassName { get; set; }
    public string? TeamName { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public EventEntity? Event { get; set; }
    public UserEntity? User { get; set; }
    public CarEntity? Car { get; set; }
    public StartListEntity? StartList { get; set; }
    public ResultEntity? Result { get; set; }
}

public sealed class EventJudgeEntity
{
    public int EventJudgeId { get; set; }
    public int EventId { get; set; }
    public int JudgeUserId { get; set; }
    public DateTime AssignedAt { get; set; }
    public EventEntity? Event { get; set; }
    public UserEntity? JudgeUser { get; set; }
}

public sealed class StartListEntity
{
    public int StartListId { get; set; }
    public int EventId { get; set; }
    public int RegistrationId { get; set; }
    public int? StartNumber { get; set; }
    public int? StartPosition { get; set; }
    public EventEntity? Event { get; set; }
    public RegistrationEntity? Registration { get; set; }
}

public sealed class ResultEntity
{
    public int ResultId { get; set; }
    public string? ExternalId { get; set; }
    public int EventId { get; set; }
    public int RegistrationId { get; set; }
    public int? Position { get; set; }
    public int? Lap1Ms { get; set; }
    public int? Lap2Ms { get; set; }
    public int? Lap3Ms { get; set; }
    public int? BestLapTimeMs { get; set; }
    public int? TotalTimeMs { get; set; }
    public int? GapMs { get; set; }
    public int? PenaltyMs { get; set; }
    public int? FinalTimeMs { get; set; }
    public string? ClassName { get; set; }
    public string? CarName { get; set; }
    public decimal Points { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public EventEntity? Event { get; set; }
    public RegistrationEntity? Registration { get; set; }
    public ICollection<PenaltyEntity> Penalties { get; set; } = new List<PenaltyEntity>();
}

public sealed class PenaltyEntity
{
    public int PenaltyId { get; set; }
    public int ResultId { get; set; }
    public int JudgeUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string PenaltyType { get; set; } = string.Empty;
    public decimal? TimeSeconds { get; set; }
    public decimal? Points { get; set; }
    public DateTime CreatedAt { get; set; }
    public ResultEntity? Result { get; set; }
    public UserEntity? JudgeUser { get; set; }
}

public sealed class ChampionshipStandingEntity
{
    public int ChampionshipStandingId { get; set; }
    public int ChampionshipId { get; set; }
    public int DriverId { get; set; }
    public int Position { get; set; }
    public decimal TotalPoints { get; set; }
}

public sealed class TeamStandingEntity
{
    public int TeamStandingId { get; set; }
    public int ChampionshipId { get; set; }
    public int TeamId { get; set; }
    public int Position { get; set; }
    public decimal TotalPoints { get; set; }
}

public sealed class MediaItemEntity
{
    public int MediaItemId { get; set; }
    public int? EventId { get; set; }
    public int? TeamId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? PreviewUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class SupportTicketEntity
{
    public int SupportTicketId { get; set; }
    public string? ExternalId { get; set; }
    public int? UserId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public ICollection<SupportMessageEntity> Messages { get; set; } = new List<SupportMessageEntity>();
}

public sealed class SupportMessageEntity
{
    public int SupportMessageId { get; set; }
    public string? ExternalId { get; set; }
    public int SupportTicketId { get; set; }
    public int AdminUserId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? EmailHtml { get; set; }
    public string? EmailDeliveryStatus { get; set; }
    public string? EmailDeliveryError { get; set; }
    public DateTime CreatedAt { get; set; }
    public SupportTicketEntity? Ticket { get; set; }
}

public sealed class PointsRuleEntity
{
    public int PointsRuleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Position { get; set; }
    public decimal Points { get; set; }
}


public sealed class ClassRuleEntity
{
    public int ClassRuleId { get; set; }
    public int? ChampionshipId { get; set; }
    public int DisciplineId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Mode { get; set; }
    public decimal MinTimeSeconds { get; set; }
    public decimal MaxTimeSeconds { get; set; }
    public bool IsElectricOnly { get; set; }
    public DateTime CreatedAt { get; set; }
    public ChampionshipEntity? Championship { get; set; }
    public DisciplineEntity? Discipline { get; set; }
}
