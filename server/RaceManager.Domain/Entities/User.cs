namespace RaceManager.Domain.Entities;

public sealed class User
{
    public string Id { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "Пользователь";
    public UserProfile Profile { get; set; } = new();
    public UserStatistics Statistics { get; set; } = new();
    public List<ChampionshipParticipation> Championships { get; set; } = [];
    public TeamInfo? Team { get; set; }
    public List<Vehicle> Vehicles { get; set; } = [];
    public List<UserApplication> Applications { get; set; } = [];
    public string Avatar { get; set; } = string.Empty;
}

public sealed class UserProfile
{
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string MiddleName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string BirthDate { get; set; } = string.Empty;
    public string Car { get; set; } = string.Empty;
    public string DriverNumber { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public string OrganizationColor { get; set; } = "#e10600";
    public string OrganizationLogo { get; set; } = string.Empty;
    public string OrganizationBanner { get; set; } = string.Empty;
    public List<OrganizationMember> OrganizationMembers { get; set; } = [];
    public string RacingTeamName { get; set; } = string.Empty;
    public string RacingTeamColor { get; set; } = "#e10600";
    public string RacingTeamLogo { get; set; } = string.Empty;
    public string RacingTeamBanner { get; set; } = string.Empty;
    public List<OrganizationMember> RacingTeamMembers { get; set; } = [];
    public List<TeamInvitation> TeamInvitations { get; set; } = [];
    public List<TeamNotification> TeamNotifications { get; set; } = [];
    public List<TeamMembership> TeamMemberships { get; set; } = [];
}

public sealed class TeamInvitation
{
    public string Id { get; set; } = string.Empty;
    public string OwnerUserId { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string TeamType { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public string TeamColor { get; set; } = "#e10600";
    public string TeamLogo { get; set; } = string.Empty;
    public string TeamBanner { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class TeamNotification
{
    public string Id { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class TeamMembership
{
    public string OwnerUserId { get; set; } = string.Empty;
    public string TeamType { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public string TeamColor { get; set; } = "#e10600";
    public string TeamLogo { get; set; } = string.Empty;
    public string TeamBanner { get; set; } = string.Empty;
}


public sealed class OrganizationMember
{
    public string UserId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public sealed class UserStatistics
{
    public int Ranking { get; set; } = 1;
    public int Points { get; set; }
    public int Races { get; set; }
    public int Wins { get; set; }
    public int Podiums { get; set; }
    public int Qualifications { get; set; }
    public string BestResult { get; set; } = "—";
    public int FinishedEvents { get; set; }
}

public sealed class ChampionshipParticipation
{
    public string Name { get; set; } = string.Empty;
    public string Discipline { get; set; } = string.Empty;
    public string Season { get; set; } = string.Empty;
    public int Position { get; set; }
    public int Points { get; set; }
    public int Events { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}

public sealed class TeamInfo
{
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int Points { get; set; }
    public int Position { get; set; }
    public string Logo { get; set; } = string.Empty;
    public string Page { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}

public sealed class Vehicle
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public Dictionary<string, string> Specs { get; set; } = [];
    public bool IsTeamVehicle { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public string TeamLogo { get; set; } = string.Empty;
}

public sealed class UserApplication
{
    public string Id { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Discipline { get; set; } = string.Empty;
    public string Status { get; set; } = "На рассмотрении";
}
