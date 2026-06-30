IF DB_ID(N'RaceManagerDb') IS NULL
BEGIN
    CREATE DATABASE RaceManagerDb;
END
GO

USE RaceManagerDb;
GO

/* Drop order for development reset. Uncomment only when you intentionally need a clean database.
DROP TABLE IF EXISTS dbo.SupportMessages;
DROP TABLE IF EXISTS dbo.SupportTickets;
DROP TABLE IF EXISTS dbo.MediaItems;
DROP TABLE IF EXISTS dbo.TeamStandings;
DROP TABLE IF EXISTS dbo.ChampionshipStandings;
DROP TABLE IF EXISTS dbo.Penalties;
DROP TABLE IF EXISTS dbo.Results;
DROP TABLE IF EXISTS dbo.StartLists;
DROP TABLE IF EXISTS dbo.EventJudges;
DROP TABLE IF EXISTS dbo.Registrations;
DROP TABLE IF EXISTS dbo.Events;
DROP TABLE IF EXISTS dbo.Championships;
DROP TABLE IF EXISTS dbo.Cars;
DROP TABLE IF EXISTS dbo.Drivers;
DROP TABLE IF EXISTS dbo.Tracks;
DROP TABLE IF EXISTS dbo.Teams;
DROP TABLE IF EXISTS dbo.Users;
DROP TABLE IF EXISTS dbo.Disciplines;
DROP TABLE IF EXISTS dbo.Roles;
GO
*/

IF OBJECT_ID(N'dbo.Roles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Roles
    (
        RoleId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Roles PRIMARY KEY,
        Name NVARCHAR(60) NOT NULL CONSTRAINT UQ_Roles_Name UNIQUE,
        DisplayName NVARCHAR(100) NOT NULL
    );
END
GO

IF OBJECT_ID(N'dbo.Disciplines', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Disciplines
    (
        DisciplineId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Disciplines PRIMARY KEY,
        Name NVARCHAR(80) NOT NULL CONSTRAINT UQ_Disciplines_Name UNIQUE,
        DisplayName NVARCHAR(120) NOT NULL,
        AccentColor NVARCHAR(20) NULL
    );
END
GO


IF OBJECT_ID(N'dbo.ClassRules', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ClassRules
    (
        ClassRuleId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ClassRules PRIMARY KEY,
        ChampionshipId INT NULL,
        DisciplineId INT NOT NULL,
        Name NVARCHAR(120) NOT NULL,
        Mode NVARCHAR(80) NULL,
        MinTimeSeconds DECIMAL(8,3) NOT NULL,
        MaxTimeSeconds DECIMAL(8,3) NOT NULL,
        IsElectricOnly BIT NOT NULL CONSTRAINT DF_ClassRules_IsElectricOnly DEFAULT 0,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_ClassRules_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_ClassRules_Disciplines FOREIGN KEY (DisciplineId) REFERENCES dbo.Disciplines(DisciplineId),
        CONSTRAINT CK_ClassRules_TimeRange CHECK (MinTimeSeconds <= MaxTimeSeconds)
    );
END
GO

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        UserId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
        ExternalId NVARCHAR(120) NULL,
        RoleId INT NOT NULL,
        Login NVARCHAR(80) NOT NULL,
        Email NVARCHAR(256) NOT NULL,
        PasswordHash NVARCHAR(512) NOT NULL,
        LastName NVARCHAR(80) NULL,
        FirstName NVARCHAR(80) NULL,
        MiddleName NVARCHAR(80) NULL,
        Phone NVARCHAR(40) NULL,
        AvatarUrl NVARCHAR(MAX) NULL,
        OrganizationName NVARCHAR(160) NULL,
        OrganizationColor NVARCHAR(20) NULL,
        OrganizationLogoUrl NVARCHAR(MAX) NULL,
        OrganizationBannerUrl NVARCHAR(MAX) NULL,
        OrganizationMembersJson NVARCHAR(MAX) NULL,
        RacingTeamName NVARCHAR(160) NULL,
        RacingTeamColor NVARCHAR(20) NULL,
        RacingTeamLogoUrl NVARCHAR(MAX) NULL,
        RacingTeamBannerUrl NVARCHAR(MAX) NULL,
        RacingTeamMembersJson NVARCHAR(MAX) NULL,
        TeamInvitationsJson NVARCHAR(MAX) NULL,
        TeamNotificationsJson NVARCHAR(MAX) NULL,
        TeamMembershipsJson NVARCHAR(MAX) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2(0) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT 1,
        CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles(RoleId),
        CONSTRAINT UQ_Users_Login UNIQUE (Login),
        CONSTRAINT UQ_Users_Email UNIQUE (Email)
    );
END
GO

IF OBJECT_ID(N'dbo.Teams', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Teams
    (
        TeamId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Teams PRIMARY KEY,
        Name NVARCHAR(160) NOT NULL,
        LogoUrl NVARCHAR(500) NULL,
        Description NVARCHAR(MAX) NULL,
        SocialUrl NVARCHAR(500) NULL,
        AccentColor NVARCHAR(20) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Teams_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UQ_Teams_Name UNIQUE (Name)
    );
END
GO

IF OBJECT_ID(N'dbo.Drivers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Drivers
    (
        DriverId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Drivers PRIMARY KEY,
        UserId INT NOT NULL,
        TeamId INT NULL,
        LicenseNumber NVARCHAR(80) NULL,
        DriverNumber NVARCHAR(20) NULL,
        RatingPosition INT NULL,
        TotalPoints DECIMAL(10,2) NOT NULL CONSTRAINT DF_Drivers_TotalPoints DEFAULT 0,
        RacesCount INT NOT NULL CONSTRAINT DF_Drivers_RacesCount DEFAULT 0,
        WinsCount INT NOT NULL CONSTRAINT DF_Drivers_WinsCount DEFAULT 0,
        PodiumsCount INT NOT NULL CONSTRAINT DF_Drivers_PodiumsCount DEFAULT 0,
        Bio NVARCHAR(MAX) NULL,
        CONSTRAINT FK_Drivers_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_Drivers_Teams FOREIGN KEY (TeamId) REFERENCES dbo.Teams(TeamId),
        CONSTRAINT UQ_Drivers_UserId UNIQUE (UserId)
    );
END
GO

IF OBJECT_ID(N'dbo.Cars', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cars
    (
        CarId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Cars PRIMARY KEY,
        ExternalId NVARCHAR(120) NULL,
        UserId INT NOT NULL,
        Name NVARCHAR(160) NOT NULL,
        ImageUrl NVARCHAR(MAX) NULL,
        Type NVARCHAR(80) NULL,
        PowerHp INT NULL,
        WeightKg INT NULL,
        PowerToWeight DECIMAL(10,2) NULL,
        DriveType NVARCHAR(80) NULL,
        EngineType NVARCHAR(80) NULL,
        EngineModel NVARCHAR(120) NULL,
        EngineVolumeCm3 INT NULL,
        TorqueNm INT NULL,
        IsFavorite BIT NOT NULL CONSTRAINT DF_Cars_IsFavorite DEFAULT 0,
        IsTeamVehicle BIT NOT NULL CONSTRAINT DF_Cars_IsTeamVehicle DEFAULT 0,
        TeamName NVARCHAR(160) NULL,
        TeamLogoUrl NVARCHAR(MAX) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Cars_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_Cars_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT CK_Cars_PowerHp CHECK (PowerHp IS NULL OR PowerHp >= 0),
        CONSTRAINT CK_Cars_WeightKg CHECK (WeightKg IS NULL OR WeightKg >= 0),
        CONSTRAINT CK_Cars_PowerToWeight CHECK (PowerToWeight IS NULL OR PowerToWeight >= 0)
    );
END
GO

IF OBJECT_ID(N'dbo.Tracks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Tracks
    (
        TrackId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Tracks PRIMARY KEY,
        Name NVARCHAR(160) NOT NULL,
        Location NVARCHAR(200) NULL,
        ConfigurationName NVARCHAR(160) NULL,
        LengthMeters INT NULL,
        ImageUrl NVARCHAR(500) NULL,
        Description NVARCHAR(MAX) NULL,
        CONSTRAINT CK_Tracks_LengthMeters CHECK (LengthMeters IS NULL OR LengthMeters > 0)
    );
END
GO

IF OBJECT_ID(N'dbo.Championships', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Championships
    (
        ChampionshipId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Championships PRIMARY KEY,
        OrganizerId INT NOT NULL,
        DisciplineId INT NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(MAX) NULL,
        SeasonYear INT NOT NULL,
        BannerUrl NVARCHAR(MAX) NULL,
        RegulationFileUrl NVARCHAR(500) NULL,
        Status NVARCHAR(40) NOT NULL CONSTRAINT DF_Championships_Status DEFAULT N'Draft',
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Championships_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2(0) NULL,
        CONSTRAINT FK_Championships_Users_Organizer FOREIGN KEY (OrganizerId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_Championships_Disciplines FOREIGN KEY (DisciplineId) REFERENCES dbo.Disciplines(DisciplineId),
        CONSTRAINT CK_Championships_Status CHECK (Status IN (N'Draft', N'Published', N'Active', N'Completed', N'Cancelled')),
        CONSTRAINT CK_Championships_SeasonYear CHECK (SeasonYear BETWEEN 2000 AND 2100)
    );
END
GO

IF OBJECT_ID(N'dbo.Events', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Events
    (
        EventId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Events PRIMARY KEY,
        ExternalId NVARCHAR(120) NULL,
        ChampionshipId INT NULL,
        OrganizerId INT NOT NULL,
        TrackId INT NULL,
        DisciplineId INT NOT NULL,
        Name NVARCHAR(220) NOT NULL,
        Type NVARCHAR(40) NOT NULL,
        StageNumber INT NULL,
        Description NVARCHAR(MAX) NULL,
        DateStart DATETIME2(0) NOT NULL,
        DateEnd DATETIME2(0) NULL,
        RegistrationStatus NVARCHAR(40) NOT NULL CONSTRAINT DF_Events_RegistrationStatus DEFAULT N'Closed',
        MaxParticipants INT NULL,
        LapsCount INT NULL,
        DistanceMeters INT NULL,
        TrackConfigImageUrl NVARCHAR(MAX) NULL,
        BannerUrl NVARCHAR(MAX) NULL,
        CalendarBannerUrl NVARCHAR(MAX) NULL,
        OrganizerName NVARCHAR(160) NULL,
        OrganizerColor NVARCHAR(20) NULL,
        OrganizerLogoUrl NVARCHAR(MAX) NULL,
        StagesJson NVARCHAR(MAX) NULL,
        RegulationFileUrl NVARCHAR(500) NULL,
        Status NVARCHAR(40) NOT NULL CONSTRAINT DF_Events_Status DEFAULT N'Draft',
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Events_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2(0) NULL,
        CONSTRAINT FK_Events_Championships FOREIGN KEY (ChampionshipId) REFERENCES dbo.Championships(ChampionshipId),
        CONSTRAINT FK_Events_Users_Organizer FOREIGN KEY (OrganizerId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_Events_Tracks FOREIGN KEY (TrackId) REFERENCES dbo.Tracks(TrackId),
        CONSTRAINT FK_Events_Disciplines FOREIGN KEY (DisciplineId) REFERENCES dbo.Disciplines(DisciplineId),
        CONSTRAINT CK_Events_Type CHECK (Type IN (N'ChampionshipStage', N'Training', N'TrackDay')),
        CONSTRAINT CK_Events_RegistrationStatus CHECK (RegistrationStatus IN (N'Open', N'Closed', N'ComingSoon', N'Completed')),
        CONSTRAINT CK_Events_Status CHECK (Status IN (N'Draft', N'Published', N'Active', N'Completed', N'Cancelled')),
        CONSTRAINT CK_Events_MaxParticipants CHECK (MaxParticipants IS NULL OR MaxParticipants > 0),
        CONSTRAINT CK_Events_LapsCount CHECK (LapsCount IS NULL OR LapsCount > 0),
        CONSTRAINT CK_Events_DistanceMeters CHECK (DistanceMeters IS NULL OR DistanceMeters > 0),
        CONSTRAINT CK_Events_DateEnd CHECK (DateEnd IS NULL OR DateEnd >= DateStart)
    );
END
GO

IF OBJECT_ID(N'dbo.Registrations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Registrations
    (
        RegistrationId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Registrations PRIMARY KEY,
        EventId INT NOT NULL,
        UserId INT NOT NULL,
        CarId INT NULL,
        ManualCarName NVARCHAR(160) NULL,
        FullName NVARCHAR(240) NOT NULL,
        Email NVARCHAR(256) NOT NULL,
        Phone NVARCHAR(40) NULL,
        TeamName NVARCHAR(160) NULL,
        DriverNumber NVARCHAR(20) NULL,
        Status NVARCHAR(40) NOT NULL CONSTRAINT DF_Registrations_Status DEFAULT N'Pending',
        RegisteredAt DATETIME2(0) NOT NULL CONSTRAINT DF_Registrations_RegisteredAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2(0) NULL,
        CONSTRAINT FK_Registrations_Events FOREIGN KEY (EventId) REFERENCES dbo.Events(EventId),
        CONSTRAINT FK_Registrations_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_Registrations_Cars FOREIGN KEY (CarId) REFERENCES dbo.Cars(CarId),
        CONSTRAINT CK_Registrations_Status CHECK (Status IN (N'Pending', N'Approved', N'Declined', N'Withdrawn')),
        CONSTRAINT UQ_Registrations_Event_User UNIQUE (EventId, UserId)
    );
END
GO

IF OBJECT_ID(N'dbo.EventJudges', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EventJudges
    (
        EventJudgeId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_EventJudges PRIMARY KEY,
        EventId INT NOT NULL,
        JudgeUserId INT NOT NULL,
        AssignedAt DATETIME2(0) NOT NULL CONSTRAINT DF_EventJudges_AssignedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_EventJudges_Events FOREIGN KEY (EventId) REFERENCES dbo.Events(EventId),
        CONSTRAINT FK_EventJudges_Users_Judge FOREIGN KEY (JudgeUserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT UQ_EventJudges_Event_Judge UNIQUE (EventId, JudgeUserId)
    );
END
GO

IF OBJECT_ID(N'dbo.StartLists', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StartLists
    (
        StartListId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_StartLists PRIMARY KEY,
        EventId INT NOT NULL,
        RegistrationId INT NOT NULL,
        StartNumber INT NULL,
        StartPosition INT NULL,
        CONSTRAINT FK_StartLists_Events FOREIGN KEY (EventId) REFERENCES dbo.Events(EventId),
        CONSTRAINT FK_StartLists_Registrations FOREIGN KEY (RegistrationId) REFERENCES dbo.Registrations(RegistrationId),
        CONSTRAINT UQ_StartLists_Registration UNIQUE (RegistrationId),
        CONSTRAINT UQ_StartLists_Event_StartNumber UNIQUE (EventId, StartNumber),
        CONSTRAINT UQ_StartLists_Event_StartPosition UNIQUE (EventId, StartPosition),
        CONSTRAINT CK_StartLists_StartNumber CHECK (StartNumber IS NULL OR StartNumber > 0),
        CONSTRAINT CK_StartLists_StartPosition CHECK (StartPosition IS NULL OR StartPosition > 0)
    );
END
GO

IF OBJECT_ID(N'dbo.Results', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Results
    (
        ResultId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Results PRIMARY KEY,
        ExternalId NVARCHAR(120) NULL,
        EventId INT NOT NULL,
        RegistrationId INT NOT NULL,
        Position INT NULL,
        BestLapTimeMs INT NULL,
        TotalTimeMs INT NULL,
        GapMs INT NULL,
        Points DECIMAL(10,2) NOT NULL CONSTRAINT DF_Results_Points DEFAULT 0,
        Status NVARCHAR(40) NOT NULL CONSTRAINT DF_Results_Status DEFAULT N'Finished',
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Results_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2(0) NULL,
        CONSTRAINT FK_Results_Events FOREIGN KEY (EventId) REFERENCES dbo.Events(EventId),
        CONSTRAINT FK_Results_Registrations FOREIGN KEY (RegistrationId) REFERENCES dbo.Registrations(RegistrationId),
        CONSTRAINT UQ_Results_Registration UNIQUE (RegistrationId),
        CONSTRAINT CK_Results_Status CHECK (Status IN (N'Finished', N'DNF', N'DSQ', N'Pending')),
        CONSTRAINT CK_Results_Position CHECK (Position IS NULL OR Position > 0),
        CONSTRAINT CK_Results_BestLapTimeMs CHECK (BestLapTimeMs IS NULL OR BestLapTimeMs >= 0),
        CONSTRAINT CK_Results_TotalTimeMs CHECK (TotalTimeMs IS NULL OR TotalTimeMs >= 0),
        CONSTRAINT CK_Results_GapMs CHECK (GapMs IS NULL OR GapMs >= 0),
        CONSTRAINT CK_Results_Points CHECK (Points >= 0)
    );
END
GO

IF OBJECT_ID(N'dbo.Penalties', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Penalties
    (
        PenaltyId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Penalties PRIMARY KEY,
        ResultId INT NOT NULL,
        JudgeUserId INT NOT NULL,
        Reason NVARCHAR(500) NOT NULL,
        PenaltyType NVARCHAR(40) NOT NULL,
        TimeSeconds DECIMAL(10,2) NULL,
        Points DECIMAL(10,2) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Penalties_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_Penalties_Results FOREIGN KEY (ResultId) REFERENCES dbo.Results(ResultId),
        CONSTRAINT FK_Penalties_Users_Judge FOREIGN KEY (JudgeUserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT CK_Penalties_Type CHECK (PenaltyType IN (N'TimePenalty', N'PointsPenalty', N'Disqualification')),
        CONSTRAINT CK_Penalties_TimeSeconds CHECK (TimeSeconds IS NULL OR TimeSeconds >= 0),
        CONSTRAINT CK_Penalties_Points CHECK (Points IS NULL OR Points >= 0)
    );
END
GO

IF OBJECT_ID(N'dbo.ChampionshipStandings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChampionshipStandings
    (
        ChampionshipStandingId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ChampionshipStandings PRIMARY KEY,
        ChampionshipId INT NOT NULL,
        DriverId INT NOT NULL,
        Position INT NOT NULL,
        TotalPoints DECIMAL(10,2) NOT NULL CONSTRAINT DF_ChampionshipStandings_TotalPoints DEFAULT 0,
        CONSTRAINT FK_ChampionshipStandings_Championships FOREIGN KEY (ChampionshipId) REFERENCES dbo.Championships(ChampionshipId),
        CONSTRAINT FK_ChampionshipStandings_Drivers FOREIGN KEY (DriverId) REFERENCES dbo.Drivers(DriverId),
        CONSTRAINT UQ_ChampionshipStandings_Championship_Driver UNIQUE (ChampionshipId, DriverId),
        CONSTRAINT UQ_ChampionshipStandings_Championship_Position UNIQUE (ChampionshipId, Position),
        CONSTRAINT CK_ChampionshipStandings_Position CHECK (Position > 0),
        CONSTRAINT CK_ChampionshipStandings_TotalPoints CHECK (TotalPoints >= 0)
    );
END
GO

IF OBJECT_ID(N'dbo.TeamStandings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TeamStandings
    (
        TeamStandingId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TeamStandings PRIMARY KEY,
        ChampionshipId INT NOT NULL,
        TeamId INT NOT NULL,
        Position INT NOT NULL,
        TotalPoints DECIMAL(10,2) NOT NULL CONSTRAINT DF_TeamStandings_TotalPoints DEFAULT 0,
        CONSTRAINT FK_TeamStandings_Championships FOREIGN KEY (ChampionshipId) REFERENCES dbo.Championships(ChampionshipId),
        CONSTRAINT FK_TeamStandings_Teams FOREIGN KEY (TeamId) REFERENCES dbo.Teams(TeamId),
        CONSTRAINT UQ_TeamStandings_Championship_Team UNIQUE (ChampionshipId, TeamId),
        CONSTRAINT UQ_TeamStandings_Championship_Position UNIQUE (ChampionshipId, Position),
        CONSTRAINT CK_TeamStandings_Position CHECK (Position > 0),
        CONSTRAINT CK_TeamStandings_TotalPoints CHECK (TotalPoints >= 0)
    );
END
GO

IF OBJECT_ID(N'dbo.MediaItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MediaItems
    (
        MediaItemId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MediaItems PRIMARY KEY,
        EventId INT NULL,
        TeamId INT NULL,
        Title NVARCHAR(220) NOT NULL,
        Type NVARCHAR(40) NOT NULL,
        Url NVARCHAR(800) NOT NULL,
        PreviewUrl NVARCHAR(800) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_MediaItems_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_MediaItems_Events FOREIGN KEY (EventId) REFERENCES dbo.Events(EventId),
        CONSTRAINT FK_MediaItems_Teams FOREIGN KEY (TeamId) REFERENCES dbo.Teams(TeamId),
        CONSTRAINT CK_MediaItems_Type CHECK (Type IN (N'Photo', N'Video')),
        CONSTRAINT CK_MediaItems_Owner CHECK (EventId IS NOT NULL OR TeamId IS NOT NULL)
    );
END
GO

IF OBJECT_ID(N'dbo.SupportTickets', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SupportTickets
    (
        SupportTicketId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SupportTickets PRIMARY KEY,
        ExternalId NVARCHAR(120) NULL,
        UserId INT NULL,
        SenderName NVARCHAR(160) NOT NULL,
        SenderEmail NVARCHAR(256) NOT NULL,
        Subject NVARCHAR(220) NOT NULL,
        Category NVARCHAR(160) NULL,
        Message NVARCHAR(MAX) NOT NULL,
        Status NVARCHAR(40) NOT NULL CONSTRAINT DF_SupportTickets_Status DEFAULT N'Waiting',
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_SupportTickets_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2(0) NULL,
        CONSTRAINT FK_SupportTickets_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT CK_SupportTickets_Status CHECK (Status IN (N'Waiting', N'Reviewed', N'Rejected'))
    );
END
GO

IF OBJECT_ID(N'dbo.SupportMessages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SupportMessages
    (
        SupportMessageId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SupportMessages PRIMARY KEY,
        ExternalId NVARCHAR(120) NULL,
        SupportTicketId INT NOT NULL,
        AdminUserId INT NOT NULL,
        Message NVARCHAR(MAX) NOT NULL,
        EmailHtml NVARCHAR(MAX) NULL,
        EmailDeliveryStatus NVARCHAR(80) NULL,
        EmailDeliveryError NVARCHAR(MAX) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_SupportMessages_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_SupportMessages_SupportTickets FOREIGN KEY (SupportTicketId) REFERENCES dbo.SupportTickets(SupportTicketId),
        CONSTRAINT FK_SupportMessages_Users_Admin FOREIGN KEY (AdminUserId) REFERENCES dbo.Users(UserId)
    );
END
GO


/* Event images are stored as Base64 data URLs by the current web client. */
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Championships') AND name = N'BannerUrl' AND max_length <> -1)
    ALTER TABLE dbo.Championships ALTER COLUMN BannerUrl NVARCHAR(MAX) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Events') AND name = N'BannerUrl' AND max_length <> -1)
    ALTER TABLE dbo.Events ALTER COLUMN BannerUrl NVARCHAR(MAX) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Events') AND name = N'TrackConfigImageUrl' AND max_length <> -1)
    ALTER TABLE dbo.Events ALTER COLUMN TrackConfigImageUrl NVARCHAR(MAX) NULL;
GO

IF COL_LENGTH('dbo.Users', 'OrganizationName') IS NULL ALTER TABLE dbo.Users ADD OrganizationName NVARCHAR(160) NULL;
GO
IF COL_LENGTH('dbo.Users', 'OrganizationColor') IS NULL ALTER TABLE dbo.Users ADD OrganizationColor NVARCHAR(20) NULL;
GO
IF COL_LENGTH('dbo.Users', 'OrganizationLogoUrl') IS NULL ALTER TABLE dbo.Users ADD OrganizationLogoUrl NVARCHAR(MAX) NULL;
GO
IF COL_LENGTH('dbo.Users', 'OrganizationBannerUrl') IS NULL ALTER TABLE dbo.Users ADD OrganizationBannerUrl NVARCHAR(MAX) NULL;
GO
IF COL_LENGTH('dbo.Users', 'OrganizationMembersJson') IS NULL ALTER TABLE dbo.Users ADD OrganizationMembersJson NVARCHAR(MAX) NULL;
GO
IF COL_LENGTH('dbo.Users', 'RacingTeamName') IS NULL ALTER TABLE dbo.Users ADD RacingTeamName NVARCHAR(160) NULL;
GO
IF COL_LENGTH('dbo.Users', 'RacingTeamColor') IS NULL ALTER TABLE dbo.Users ADD RacingTeamColor NVARCHAR(20) NULL;
GO
IF COL_LENGTH('dbo.Users', 'RacingTeamLogoUrl') IS NULL ALTER TABLE dbo.Users ADD RacingTeamLogoUrl NVARCHAR(MAX) NULL;
GO
IF COL_LENGTH('dbo.Users', 'RacingTeamBannerUrl') IS NULL ALTER TABLE dbo.Users ADD RacingTeamBannerUrl NVARCHAR(MAX) NULL;
GO
IF COL_LENGTH('dbo.Users', 'RacingTeamMembersJson') IS NULL ALTER TABLE dbo.Users ADD RacingTeamMembersJson NVARCHAR(MAX) NULL;
GO
IF COL_LENGTH('dbo.Users', 'TeamInvitationsJson') IS NULL ALTER TABLE dbo.Users ADD TeamInvitationsJson NVARCHAR(MAX) NULL;
GO
IF COL_LENGTH('dbo.Users', 'TeamNotificationsJson') IS NULL ALTER TABLE dbo.Users ADD TeamNotificationsJson NVARCHAR(MAX) NULL;
GO
IF COL_LENGTH('dbo.Users', 'TeamMembershipsJson') IS NULL ALTER TABLE dbo.Users ADD TeamMembershipsJson NVARCHAR(MAX) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Users') AND name = N'AvatarUrl' AND max_length <> -1)
    ALTER TABLE dbo.Users ALTER COLUMN AvatarUrl NVARCHAR(MAX) NULL;
GO
IF COL_LENGTH('dbo.Events', 'CalendarBannerUrl') IS NULL ALTER TABLE dbo.Events ADD CalendarBannerUrl NVARCHAR(MAX) NULL;
GO
IF COL_LENGTH('dbo.Events', 'OrganizerName') IS NULL ALTER TABLE dbo.Events ADD OrganizerName NVARCHAR(160) NULL;
GO
IF COL_LENGTH('dbo.Events', 'OrganizerColor') IS NULL ALTER TABLE dbo.Events ADD OrganizerColor NVARCHAR(20) NULL;
GO
IF COL_LENGTH('dbo.Events', 'OrganizerLogoUrl') IS NULL ALTER TABLE dbo.Events ADD OrganizerLogoUrl NVARCHAR(MAX) NULL;
GO
IF COL_LENGTH('dbo.Events', 'StagesJson') IS NULL ALTER TABLE dbo.Events ADD StagesJson NVARCHAR(MAX) NULL;
GO

IF COL_LENGTH('dbo.Cars', 'IsTeamVehicle') IS NULL ALTER TABLE dbo.Cars ADD IsTeamVehicle BIT NOT NULL CONSTRAINT DF_Cars_IsTeamVehicle_Migration DEFAULT 0;
GO
IF COL_LENGTH('dbo.Cars', 'TeamName') IS NULL ALTER TABLE dbo.Cars ADD TeamName NVARCHAR(160) NULL;
GO
IF COL_LENGTH('dbo.Cars', 'TeamLogoUrl') IS NULL ALTER TABLE dbo.Cars ADD TeamLogoUrl NVARCHAR(MAX) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Cars') AND name = N'ImageUrl' AND max_length <> -1)
    ALTER TABLE dbo.Cars ALTER COLUMN ImageUrl NVARCHAR(MAX) NULL;
GO

IF COL_LENGTH('dbo.Events', 'ExternalId') IS NULL ALTER TABLE dbo.Events ADD ExternalId NVARCHAR(120) NULL;
GO
IF COL_LENGTH('dbo.Results', 'ExternalId') IS NULL ALTER TABLE dbo.Results ADD ExternalId NVARCHAR(120) NULL;
GO

IF COL_LENGTH('dbo.Registrations', 'TeamName') IS NULL ALTER TABLE dbo.Registrations ADD TeamName NVARCHAR(160) NULL;
GO

IF COL_LENGTH('dbo.Drivers', 'DriverNumber') IS NULL ALTER TABLE dbo.Drivers ADD DriverNumber NVARCHAR(20) NULL;
GO
IF COL_LENGTH('dbo.Registrations', 'DriverNumber') IS NULL ALTER TABLE dbo.Registrations ADD DriverNumber NVARCHAR(20) NULL;
GO

IF COL_LENGTH('dbo.Events', 'ClassMode') IS NULL ALTER TABLE dbo.Events ADD ClassMode NVARCHAR(80) NULL;
GO
IF COL_LENGTH('dbo.Registrations', 'QualificationTimeSeconds') IS NULL ALTER TABLE dbo.Registrations ADD QualificationTimeSeconds DECIMAL(8,3) NULL;
GO
IF COL_LENGTH('dbo.Registrations', 'ClassName') IS NULL ALTER TABLE dbo.Registrations ADD ClassName NVARCHAR(120) NULL;
GO
IF COL_LENGTH('dbo.Results', 'Lap1Ms') IS NULL ALTER TABLE dbo.Results ADD Lap1Ms INT NULL;
GO
IF COL_LENGTH('dbo.Results', 'Lap2Ms') IS NULL ALTER TABLE dbo.Results ADD Lap2Ms INT NULL;
GO
IF COL_LENGTH('dbo.Results', 'Lap3Ms') IS NULL ALTER TABLE dbo.Results ADD Lap3Ms INT NULL;
GO
IF COL_LENGTH('dbo.Results', 'PenaltyMs') IS NULL ALTER TABLE dbo.Results ADD PenaltyMs INT NULL;
GO
IF COL_LENGTH('dbo.Results', 'FinalTimeMs') IS NULL ALTER TABLE dbo.Results ADD FinalTimeMs INT NULL;
GO
IF COL_LENGTH('dbo.Results', 'ClassName') IS NULL ALTER TABLE dbo.Results ADD ClassName NVARCHAR(120) NULL;
GO
IF COL_LENGTH('dbo.Results', 'CarName') IS NULL ALTER TABLE dbo.Results ADD CarName NVARCHAR(160) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ClassRules_Discipline_Mode' AND object_id = OBJECT_ID(N'dbo.ClassRules'))
    CREATE INDEX IX_ClassRules_Discipline_Mode ON dbo.ClassRules(DisciplineId, Mode, MinTimeSeconds, MaxTimeSeconds);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Events_ExternalId' AND object_id = OBJECT_ID(N'dbo.Events'))
    CREATE UNIQUE INDEX UX_Events_ExternalId ON dbo.Events(ExternalId) WHERE ExternalId IS NOT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Results_ExternalId' AND object_id = OBJECT_ID(N'dbo.Results'))
    CREATE UNIQUE INDEX UX_Results_ExternalId ON dbo.Results(ExternalId) WHERE ExternalId IS NOT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Users_ExternalId' AND object_id = OBJECT_ID(N'dbo.Users'))
    CREATE UNIQUE INDEX UX_Users_ExternalId ON dbo.Users(ExternalId) WHERE ExternalId IS NOT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Cars_ExternalId' AND object_id = OBJECT_ID(N'dbo.Cars'))
    CREATE UNIQUE INDEX UX_Cars_ExternalId ON dbo.Cars(ExternalId) WHERE ExternalId IS NOT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_SupportTickets_ExternalId' AND object_id = OBJECT_ID(N'dbo.SupportTickets'))
    CREATE UNIQUE INDEX UX_SupportTickets_ExternalId ON dbo.SupportTickets(ExternalId) WHERE ExternalId IS NOT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_SupportMessages_ExternalId' AND object_id = OBJECT_ID(N'dbo.SupportMessages'))
    CREATE UNIQUE INDEX UX_SupportMessages_ExternalId ON dbo.SupportMessages(ExternalId) WHERE ExternalId IS NOT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Users_Phone' AND object_id = OBJECT_ID(N'dbo.Users'))
   AND NOT EXISTS (SELECT Phone FROM dbo.Users WHERE Phone IS NOT NULL AND LTRIM(RTRIM(Phone)) <> N'' GROUP BY Phone HAVING COUNT(*) > 1)
    CREATE UNIQUE INDEX UX_Users_Phone ON dbo.Users(Phone) WHERE Phone IS NOT NULL AND Phone <> N'';
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Users_RoleId' AND object_id = OBJECT_ID(N'dbo.Users'))
    CREATE INDEX IX_Users_RoleId ON dbo.Users(RoleId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Drivers_TeamId' AND object_id = OBJECT_ID(N'dbo.Drivers'))
    CREATE INDEX IX_Drivers_TeamId ON dbo.Drivers(TeamId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Cars_UserId' AND object_id = OBJECT_ID(N'dbo.Cars'))
    CREATE INDEX IX_Cars_UserId ON dbo.Cars(UserId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Championships_OrganizerId' AND object_id = OBJECT_ID(N'dbo.Championships'))
    CREATE INDEX IX_Championships_OrganizerId ON dbo.Championships(OrganizerId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Events_ChampionshipId' AND object_id = OBJECT_ID(N'dbo.Events'))
    CREATE INDEX IX_Events_ChampionshipId ON dbo.Events(ChampionshipId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Events_DateStart' AND object_id = OBJECT_ID(N'dbo.Events'))
    CREATE INDEX IX_Events_DateStart ON dbo.Events(DateStart);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Registrations_EventId' AND object_id = OBJECT_ID(N'dbo.Registrations'))
    CREATE INDEX IX_Registrations_EventId ON dbo.Registrations(EventId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Results_EventId_Position' AND object_id = OBJECT_ID(N'dbo.Results'))
    CREATE INDEX IX_Results_EventId_Position ON dbo.Results(EventId, Position);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SupportTickets_Status_CreatedAt' AND object_id = OBJECT_ID(N'dbo.SupportTickets'))
    CREATE INDEX IX_SupportTickets_Status_CreatedAt ON dbo.SupportTickets(Status, CreatedAt DESC);
GO


MERGE dbo.Roles AS target
USING (VALUES
    (N'User', N'Пользователь'),
    (N'Organizer', N'Организатор'),
    (N'Judge', N'Судья'),
    (N'TechnicalAdmin', N'Технический администратор')
) AS source(Name, DisplayName)
ON target.Name = source.Name
WHEN NOT MATCHED THEN
    INSERT (Name, DisplayName) VALUES (source.Name, source.DisplayName)
WHEN MATCHED THEN
    UPDATE SET DisplayName = source.DisplayName;
GO


/* Demo organizer account. The legacy value is upgraded to PBKDF2 after the first successful login. */
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Login = N'event.organizer' OR Email = N'event.organizer@racemanager.test')
BEGIN
    INSERT dbo.Users (ExternalId, RoleId, Login, Email, PasswordHash, LastName, FirstName, MiddleName, Phone, IsActive)
    SELECT N'organizer-004', RoleId, N'event.organizer', N'event.organizer@racemanager.test', N'Organizer2026!',
           N'Орлова', N'Марина', N'Андреевна', N'+375 33 555-44-55', 1
    FROM dbo.Roles
    WHERE Name = N'Organizer';
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Login = N'standard.organizer' OR Email = N'standard.organizer@racemanager.test')
BEGIN
    INSERT dbo.Users (ExternalId, RoleId, Login, Email, PasswordHash, LastName, FirstName, MiddleName, Phone, IsActive)
    SELECT N'organizer-standard', RoleId, N'standard.organizer', N'standard.organizer@racemanager.test', N'OrganizerStandard2026!',
           N'Смирнов', N'Илья', N'Павлович', N'+375 29 330-44-55', 1
    FROM dbo.Roles
    WHERE Name = N'Organizer';
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Login = N'22rt.organizer' OR Email = N'22rt@gmail.com')
BEGIN
    INSERT dbo.Users (ExternalId, RoleId, Login, Email, PasswordHash, LastName, FirstName, MiddleName, Phone, IsActive)
    SELECT N'organizer-22rt', RoleId, N'22rt.organizer', N'22rt@gmail.com', N'Login123',
           N'22RT', N'Организатор', N'', N'+375 29 220-22-22', 1
    FROM dbo.Roles
    WHERE Name = N'Organizer';
END
GO

MERGE dbo.Disciplines AS target
USING (VALUES
    (N'Drift', N'Дрифт', N'#00c864'),
    (N'DragRacing', N'Дрэг-рейсинг', N'#e10600'),
    (N'TimeAttack', N'Тайм-Аттак', N'#139bff')
) AS source(Name, DisplayName, AccentColor)
ON target.Name = source.Name
WHEN NOT MATCHED THEN
    INSERT (Name, DisplayName, AccentColor) VALUES (source.Name, source.DisplayName, source.AccentColor)
WHEN MATCHED THEN
    UPDATE SET DisplayName = source.DisplayName, AccentColor = source.AccentColor;
GO


DECLARE @DragDisciplineId INT = (SELECT DisciplineId FROM dbo.Disciplines WHERE Name = N'DragRacing');
DECLARE @TimeAttackDisciplineId INT = (SELECT DisciplineId FROM dbo.Disciplines WHERE Name = N'TimeAttack');

MERGE dbo.ClassRules AS target
USING (VALUES
    (@DragDisciplineId, N'Club Handicap', N'StandardDragHandicap', CAST(14.000 AS DECIMAL(8,3)), CAST(14.999 AS DECIMAL(8,3)), CAST(0 AS BIT)),
    (@DragDisciplineId, N'Street Handicap', N'StandardDragHandicap', CAST(13.000 AS DECIMAL(8,3)), CAST(15.000 AS DECIMAL(8,3)), CAST(0 AS BIT)),
    (@DragDisciplineId, N'Sport Handicap', N'StandardDragHandicap', CAST(11.000 AS DECIMAL(8,3)), CAST(12.999 AS DECIMAL(8,3)), CAST(0 AS BIT)),
    (@DragDisciplineId, N'Pro Handicap', N'StandardDragHandicap', CAST(9.500 AS DECIMAL(8,3)), CAST(10.999 AS DECIMAL(8,3)), CAST(0 AS BIT)),
    (@DragDisciplineId, N'Electro Handicap', N'StandardDragHandicap', CAST(9.500 AS DECIMAL(8,3)), CAST(15.000 AS DECIMAL(8,3)), CAST(1 AS BIT)),
    (@DragDisciplineId, N'Club', N'StandardDrag', CAST(14.000 AS DECIMAL(8,3)), CAST(14.999 AS DECIMAL(8,3)), CAST(0 AS BIT)),
    (@DragDisciplineId, N'Street', N'StandardDrag', CAST(13.000 AS DECIMAL(8,3)), CAST(13.999 AS DECIMAL(8,3)), CAST(0 AS BIT)),
    (@DragDisciplineId, N'Sport', N'StandardDrag', CAST(12.000 AS DECIMAL(8,3)), CAST(12.999 AS DECIMAL(8,3)), CAST(0 AS BIT)),
    (@DragDisciplineId, N'Pro', N'StandardDrag', CAST(10.000 AS DECIMAL(8,3)), CAST(10.999 AS DECIMAL(8,3)), CAST(0 AS BIT)),
    (@DragDisciplineId, N'Electro', N'StandardDrag', CAST(9.500 AS DECIMAL(8,3)), CAST(9.999 AS DECIMAL(8,3)), CAST(1 AS BIT)),
    (@TimeAttackDisciplineId, N'Stock', N'StandardTimeAttack', CAST(14.000 AS DECIMAL(8,3)), CAST(15.500 AS DECIMAL(8,3)), CAST(0 AS BIT)),
    (@TimeAttackDisciplineId, N'Street', N'StandardTimeAttack', CAST(13.000 AS DECIMAL(8,3)), CAST(13.999 AS DECIMAL(8,3)), CAST(0 AS BIT)),
    (@TimeAttackDisciplineId, N'Sport', N'StandardTimeAttack', CAST(12.000 AS DECIMAL(8,3)), CAST(12.999 AS DECIMAL(8,3)), CAST(0 AS BIT)),
    (@TimeAttackDisciplineId, N'Charged', N'StandardTimeAttack', CAST(11.000 AS DECIMAL(8,3)), CAST(11.999 AS DECIMAL(8,3)), CAST(0 AS BIT)),
    (@TimeAttackDisciplineId, N'Pro', N'StandardTimeAttack', CAST(10.000 AS DECIMAL(8,3)), CAST(10.999 AS DECIMAL(8,3)), CAST(0 AS BIT)),
    (@TimeAttackDisciplineId, N'Unlim', N'StandardTimeAttack', CAST(9.500 AS DECIMAL(8,3)), CAST(9.999 AS DECIMAL(8,3)), CAST(0 AS BIT))
) AS source(DisciplineId, Name, Mode, MinTimeSeconds, MaxTimeSeconds, IsElectricOnly)
ON target.DisciplineId = source.DisciplineId AND target.Name = source.Name AND ISNULL(target.Mode, N'') = ISNULL(source.Mode, N'')
WHEN NOT MATCHED THEN
    INSERT (DisciplineId, Name, Mode, MinTimeSeconds, MaxTimeSeconds, IsElectricOnly)
    VALUES (source.DisciplineId, source.Name, source.Mode, source.MinTimeSeconds, source.MaxTimeSeconds, source.IsElectricOnly)
WHEN MATCHED THEN
    UPDATE SET MinTimeSeconds = source.MinTimeSeconds, MaxTimeSeconds = source.MaxTimeSeconds, IsElectricOnly = source.IsElectricOnly;
GO

MERGE dbo.Teams AS target
USING (VALUES
    (N'Betera', N'/public/BeteraLogo.png', N'#00c864'),
    (N'Drift Racing Park', N'/public/DriftRacingParkLogo.png', N'#e10600'),
    (N'Blockchain Sports', N'/public/BlockchainLogo.png', N'#f5c400'),
    (N'Low Budget Drift', N'/public/LowBudgetLogo.png', N'#f1c232')
) AS source(Name, LogoUrl, AccentColor)
ON target.Name = source.Name
WHEN NOT MATCHED THEN
    INSERT (Name, LogoUrl, AccentColor, Description) VALUES (source.Name, source.LogoUrl, source.AccentColor, N'Команда RaceManager')
WHEN MATCHED THEN
    UPDATE SET LogoUrl = source.LogoUrl, AccentColor = source.AccentColor;
GO


MERGE dbo.Tracks AS target
USING (VALUES
    (N'Раубичи', N'Минская область', N'Дрифт-конфигурация', NULL, N'/public/DriftBetera1Etap.jpeg'),
    (N'Стайки', N'Минская область', N'Кольцевая/Time-Attack', NULL, N'/public/StaykiTrack.jpg'),
    (N'Гродно', N'г. Гродно', N'Городская дрифт-конфигурация', NULL, N'/public/GrodnoTrack.jpg'),
    (N'Аэропорт Могилев', N'Могилев', N'1/4 мили', 402, N'/public/KonfigDrag.png'),
    (N'Пинск', N'Пинск', N'PRO2 Drift TOP32', NULL, N'/public/PinskTrack.png')
) AS source(Name, Location, ConfigurationName, LengthMeters, ImageUrl)
ON target.Name = source.Name AND ISNULL(target.Location, N'') = ISNULL(source.Location, N'')
WHEN NOT MATCHED THEN
    INSERT (Name, Location, ConfigurationName, LengthMeters, ImageUrl) VALUES (source.Name, source.Location, source.ConfigurationName, source.LengthMeters, source.ImageUrl)
WHEN MATCHED THEN
    UPDATE SET ConfigurationName = source.ConfigurationName, LengthMeters = source.LengthMeters, ImageUrl = source.ImageUrl;
GO


IF OBJECT_ID(N'dbo.PointsRules', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PointsRules
    (
        PointsRuleId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PointsRules PRIMARY KEY,
        Name NVARCHAR(120) NOT NULL,
        Position INT NOT NULL,
        Points DECIMAL(10,2) NOT NULL,
        CONSTRAINT UQ_PointsRules_Name_Position UNIQUE (Name, Position),
        CONSTRAINT CK_PointsRules_Position CHECK (Position > 0),
        CONSTRAINT CK_PointsRules_Points CHECK (Points >= 0)
    );
END
GO

MERGE dbo.PointsRules AS target
USING (VALUES
    (N'StandardTop10', 1, 25),
    (N'StandardTop10', 2, 18),
    (N'StandardTop10', 3, 15),
    (N'StandardTop10', 4, 12),
    (N'StandardTop10', 5, 10),
    (N'StandardTop10', 6, 8),
    (N'StandardTop10', 7, 6),
    (N'StandardTop10', 8, 4),
    (N'StandardTop10', 9, 2),
    (N'StandardTop10', 10, 1)
) AS source(Name, Position, Points)
ON target.Name = source.Name AND target.Position = source.Position
WHEN NOT MATCHED THEN
    INSERT (Name, Position, Points) VALUES (source.Name, source.Position, source.Points)
WHEN MATCHED THEN
    UPDATE SET Points = source.Points;
GO

PRINT N'RaceManagerDb schema has been created or updated successfully.';
GO
