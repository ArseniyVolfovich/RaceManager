using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RaceManager.Infrastructure.Database.Models;
using RaceManager.Infrastructure.JsonStorage;

namespace RaceManager.Infrastructure.Database;

public sealed class JsonImportResult
{
    public int Users { get; set; }
    public int Drivers { get; set; }
    public int Cars { get; set; }
    public int Championships { get; set; }
    public int ChampionshipStandings { get; set; }
    public int SupportTickets { get; set; }
    public int SupportMessages { get; set; }
}

public sealed class JsonToSqlImportService(RaceManagerDbContext dbContext, JsonStorageOptions storageOptions)
{
    public async Task<JsonImportResult> ImportAsync(CancellationToken cancellationToken = default)
    {
        await EnsureImportColumnsAsync(cancellationToken);

        var result = new JsonImportResult();
        var roleIds = await dbContext.Roles.ToDictionaryAsync(x => x.Name, x => x.RoleId, cancellationToken);
        var disciplineIds = await dbContext.Disciplines.ToDictionaryAsync(x => x.Name, x => x.DisciplineId, cancellationToken);

        var usersByExternalId = new Dictionary<string, UserEntity>(StringComparer.OrdinalIgnoreCase);
        if (File.Exists(storageOptions.UsersPath))
        {
            await ImportUsersAsync(storageOptions.UsersPath, roleIds, disciplineIds, usersByExternalId, result, cancellationToken);
        }

        if (File.Exists(storageOptions.SupportTicketsPath))
        {
            await ImportSupportTicketsAsync(storageOptions.SupportTicketsPath, usersByExternalId, result, cancellationToken);
        }

        return result;
    }

    private async Task EnsureImportColumnsAsync(CancellationToken cancellationToken)
    {
        var commands = new[]
        {
            "IF COL_LENGTH('dbo.Users', 'ExternalId') IS NULL ALTER TABLE dbo.Users ADD ExternalId NVARCHAR(120) NULL;",
            "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Users_ExternalId' AND object_id = OBJECT_ID(N'dbo.Users')) CREATE UNIQUE INDEX UX_Users_ExternalId ON dbo.Users(ExternalId) WHERE ExternalId IS NOT NULL;",
            "IF COL_LENGTH('dbo.Cars', 'ExternalId') IS NULL ALTER TABLE dbo.Cars ADD ExternalId NVARCHAR(120) NULL;",
            "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Cars_ExternalId' AND object_id = OBJECT_ID(N'dbo.Cars')) CREATE UNIQUE INDEX UX_Cars_ExternalId ON dbo.Cars(ExternalId) WHERE ExternalId IS NOT NULL;",
            "IF COL_LENGTH('dbo.SupportTickets', 'ExternalId') IS NULL ALTER TABLE dbo.SupportTickets ADD ExternalId NVARCHAR(120) NULL;",
            "IF COL_LENGTH('dbo.SupportTickets', 'Category') IS NULL ALTER TABLE dbo.SupportTickets ADD Category NVARCHAR(160) NULL;",
            "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_SupportTickets_ExternalId' AND object_id = OBJECT_ID(N'dbo.SupportTickets')) CREATE UNIQUE INDEX UX_SupportTickets_ExternalId ON dbo.SupportTickets(ExternalId) WHERE ExternalId IS NOT NULL;",
            "IF COL_LENGTH('dbo.SupportMessages', 'ExternalId') IS NULL ALTER TABLE dbo.SupportMessages ADD ExternalId NVARCHAR(120) NULL;",
            "IF COL_LENGTH('dbo.SupportMessages', 'EmailHtml') IS NULL ALTER TABLE dbo.SupportMessages ADD EmailHtml NVARCHAR(MAX) NULL;",
            "IF COL_LENGTH('dbo.SupportMessages', 'EmailDeliveryStatus') IS NULL ALTER TABLE dbo.SupportMessages ADD EmailDeliveryStatus NVARCHAR(80) NULL;",
            "IF COL_LENGTH('dbo.SupportMessages', 'EmailDeliveryError') IS NULL ALTER TABLE dbo.SupportMessages ADD EmailDeliveryError NVARCHAR(MAX) NULL;",
            "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_SupportMessages_ExternalId' AND object_id = OBJECT_ID(N'dbo.SupportMessages')) CREATE UNIQUE INDEX UX_SupportMessages_ExternalId ON dbo.SupportMessages(ExternalId) WHERE ExternalId IS NOT NULL;",
            "IF OBJECT_ID(N'dbo.ClassRules', N'U') IS NULL CREATE TABLE dbo.ClassRules (ClassRuleId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ClassRules PRIMARY KEY, ChampionshipId INT NULL, DisciplineId INT NOT NULL, Name NVARCHAR(120) NOT NULL, Mode NVARCHAR(80) NULL, MinTimeSeconds DECIMAL(8,3) NOT NULL, MaxTimeSeconds DECIMAL(8,3) NOT NULL, IsElectricOnly BIT NOT NULL CONSTRAINT DF_ClassRules_IsElectricOnly DEFAULT 0, CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_ClassRules_CreatedAt DEFAULT SYSUTCDATETIME(), CONSTRAINT FK_ClassRules_Disciplines FOREIGN KEY (DisciplineId) REFERENCES dbo.Disciplines(DisciplineId), CONSTRAINT CK_ClassRules_TimeRange CHECK (MinTimeSeconds <= MaxTimeSeconds));",
            "IF COL_LENGTH('dbo.Drivers', 'DriverNumber') IS NULL ALTER TABLE dbo.Drivers ADD DriverNumber NVARCHAR(20) NULL;",
            "IF COL_LENGTH('dbo.Registrations', 'DriverNumber') IS NULL ALTER TABLE dbo.Registrations ADD DriverNumber NVARCHAR(20) NULL;",
            "IF COL_LENGTH('dbo.Events', 'ClassMode') IS NULL ALTER TABLE dbo.Events ADD ClassMode NVARCHAR(80) NULL;",
            "IF COL_LENGTH('dbo.Registrations', 'QualificationTimeSeconds') IS NULL ALTER TABLE dbo.Registrations ADD QualificationTimeSeconds DECIMAL(8,3) NULL;",
            "IF COL_LENGTH('dbo.Registrations', 'ClassName') IS NULL ALTER TABLE dbo.Registrations ADD ClassName NVARCHAR(120) NULL;",
            "IF COL_LENGTH('dbo.Results', 'Lap1Ms') IS NULL ALTER TABLE dbo.Results ADD Lap1Ms INT NULL;",
            "IF COL_LENGTH('dbo.Results', 'Lap2Ms') IS NULL ALTER TABLE dbo.Results ADD Lap2Ms INT NULL;",
            "IF COL_LENGTH('dbo.Results', 'Lap3Ms') IS NULL ALTER TABLE dbo.Results ADD Lap3Ms INT NULL;",
            "IF COL_LENGTH('dbo.Results', 'PenaltyMs') IS NULL ALTER TABLE dbo.Results ADD PenaltyMs INT NULL;",
            "IF COL_LENGTH('dbo.Results', 'FinalTimeMs') IS NULL ALTER TABLE dbo.Results ADD FinalTimeMs INT NULL;",
            "IF COL_LENGTH('dbo.Results', 'ClassName') IS NULL ALTER TABLE dbo.Results ADD ClassName NVARCHAR(120) NULL;",
            "IF COL_LENGTH('dbo.Results', 'CarName') IS NULL ALTER TABLE dbo.Results ADD CarName NVARCHAR(160) NULL;",
            "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ClassRules_Discipline_Mode' AND object_id = OBJECT_ID(N'dbo.ClassRules')) CREATE INDEX IX_ClassRules_Discipline_Mode ON dbo.ClassRules(DisciplineId, Mode, MinTimeSeconds, MaxTimeSeconds);"
        };

        foreach (var command in commands)
        {
            await dbContext.Database.ExecuteSqlRawAsync(command, cancellationToken);
        }

        await SeedClassRulesAsync(cancellationToken);
    }


    private async Task SeedClassRulesAsync(CancellationToken cancellationToken)
    {
        var dragId = await dbContext.Disciplines.Where(x => x.Name == "DragRacing").Select(x => x.DisciplineId).FirstOrDefaultAsync(cancellationToken);
        var timeAttackId = await dbContext.Disciplines.Where(x => x.Name == "TimeAttack").Select(x => x.DisciplineId).FirstOrDefaultAsync(cancellationToken);
        if (dragId == 0 || timeAttackId == 0) return;

        var rules = new (int DisciplineId, string Name, string Mode, decimal Min, decimal Max, bool ElectricOnly)[]
        {
            (dragId, "Club Handicap", "StandardDragHandicap", 14.000m, 14.999m, false),
            (dragId, "Street Handicap", "StandardDragHandicap", 13.000m, 15.000m, false),
            (dragId, "Sport Handicap", "StandardDragHandicap", 11.000m, 12.999m, false),
            (dragId, "Pro Handicap", "StandardDragHandicap", 9.500m, 10.999m, false),
            (dragId, "Electro Handicap", "StandardDragHandicap", 9.500m, 15.000m, true),
            (dragId, "Club", "StandardDrag", 14.000m, 14.999m, false),
            (dragId, "Street", "StandardDrag", 13.000m, 13.999m, false),
            (dragId, "Sport", "StandardDrag", 12.000m, 12.999m, false),
            (dragId, "Pro", "StandardDrag", 10.000m, 10.999m, false),
            (dragId, "Electro", "StandardDrag", 9.500m, 9.999m, true),
            (timeAttackId, "Stock", "StandardTimeAttack", 14.000m, 15.500m, false),
            (timeAttackId, "Street", "StandardTimeAttack", 13.000m, 13.999m, false),
            (timeAttackId, "Sport", "StandardTimeAttack", 12.000m, 12.999m, false),
            (timeAttackId, "Charged", "StandardTimeAttack", 11.000m, 11.999m, false),
            (timeAttackId, "Pro", "StandardTimeAttack", 10.000m, 10.999m, false),
            (timeAttackId, "Unlim", "StandardTimeAttack", 9.500m, 9.999m, false)
        };

        foreach (var rule in rules)
        {
            var existing = await dbContext.ClassRules.FirstOrDefaultAsync(x => x.DisciplineId == rule.DisciplineId && x.Name == rule.Name && x.Mode == rule.Mode, cancellationToken);
            if (existing is null)
            {
                dbContext.ClassRules.Add(new ClassRuleEntity
                {
                    DisciplineId = rule.DisciplineId,
                    Name = rule.Name,
                    Mode = rule.Mode,
                    MinTimeSeconds = rule.Min,
                    MaxTimeSeconds = rule.Max,
                    IsElectricOnly = rule.ElectricOnly,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                existing.MinTimeSeconds = rule.Min;
                existing.MaxTimeSeconds = rule.Max;
                existing.IsElectricOnly = rule.ElectricOnly;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ImportUsersAsync(
        string path,
        IReadOnlyDictionary<string, int> roleIds,
        IReadOnlyDictionary<string, int> disciplineIds,
        Dictionary<string, UserEntity> usersByExternalId,
        JsonImportResult result,
        CancellationToken cancellationToken)
    {
        using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(path, cancellationToken));
        if (!doc.RootElement.TryGetProperty("users", out var users) || users.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var item in users.EnumerateArray())
        {
            var externalId = GetString(item, "id");
            var login = GetString(item, "login");
            var email = GetString(item, "email");
            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(email))
            {
                continue;
            }

            var roleName = MapRole(GetString(item, "role"));
            var roleId = roleIds.TryGetValue(roleName, out var foundRoleId) ? foundRoleId : roleIds["User"];
            var profile = item.TryGetProperty("profile", out var profileElement) ? profileElement : default;

            var user = await dbContext.Users.FirstOrDefaultAsync(x => x.ExternalId == externalId, cancellationToken)
                ?? await dbContext.Users.FirstOrDefaultAsync(x => x.Login == login || x.Email == email, cancellationToken);

            if (user is null)
            {
                user = new UserEntity { CreatedAt = DateTime.UtcNow };
                dbContext.Users.Add(user);
            }

            user.ExternalId = externalId;
            user.RoleId = roleId;
            user.Login = login;
            user.Email = email;
            user.PasswordHash = GetString(item, "password");
            user.LastName = GetString(profile, "lastName");
            user.FirstName = GetString(profile, "firstName");
            user.MiddleName = GetString(profile, "middleName");
            user.Phone = GetString(profile, "phone");
            user.AvatarUrl = GetString(item, "avatar");
            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);
            result.Users++;
            if (!string.IsNullOrWhiteSpace(externalId))
            {
                usersByExternalId[externalId] = user;
            }

            if (roleName == "User")
            {
                await ImportDriverAsync(item, user, result, cancellationToken);
                await ImportCarsAsync(item, user, result, cancellationToken);
                await ImportChampionshipsAsync(item, user, disciplineIds, result, cancellationToken);
            }
        }
    }

    private async Task ImportDriverAsync(JsonElement userJson, UserEntity user, JsonImportResult result, CancellationToken cancellationToken)
    {
        var driver = await dbContext.Drivers.FirstOrDefaultAsync(x => x.UserId == user.UserId, cancellationToken);
        if (driver is null)
        {
            driver = new DriverEntity { UserId = user.UserId };
            dbContext.Drivers.Add(driver);
        }

        var teamName = userJson.TryGetProperty("team", out var teamElement) && teamElement.ValueKind == JsonValueKind.Object
            ? NormalizeTeamName(GetString(teamElement, "name"))
            : string.Empty;
        if (!string.IsNullOrWhiteSpace(teamName))
        {
            var team = await EnsureTeamAsync(teamName, GetString(teamElement, "logo"), GetString(teamElement, "color"), cancellationToken);
            driver.TeamId = team.TeamId;
        }

        var stats = userJson.TryGetProperty("statistics", out var statsElement) ? statsElement : default;
        driver.DriverNumber = GetString(userJson.TryGetProperty("profile", out var profileElement) ? profileElement : default, "driverNumber");
        driver.RatingPosition = GetInt(stats, "ranking");
        driver.TotalPoints = GetDecimal(stats, "points") ?? 0;
        driver.RacesCount = GetInt(stats, "races") ?? 0;
        driver.WinsCount = GetInt(stats, "wins") ?? 0;
        driver.PodiumsCount = GetInt(stats, "podiums") ?? 0;

        await dbContext.SaveChangesAsync(cancellationToken);
        result.Drivers++;
    }

    private async Task ImportCarsAsync(JsonElement userJson, UserEntity user, JsonImportResult result, CancellationToken cancellationToken)
    {
        if (!userJson.TryGetProperty("vehicles", out var vehicles) || vehicles.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var vehicle in vehicles.EnumerateArray())
        {
            var externalId = GetString(vehicle, "id");
            var name = GetString(vehicle, "name");
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var car = await dbContext.Cars.FirstOrDefaultAsync(x => x.ExternalId == externalId, cancellationToken)
                ?? await dbContext.Cars.FirstOrDefaultAsync(x => x.UserId == user.UserId && x.Name == name, cancellationToken);
            if (car is null)
            {
                car = new CarEntity { UserId = user.UserId, CreatedAt = DateTime.UtcNow };
                dbContext.Cars.Add(car);
            }

            car.ExternalId = externalId;
            car.Name = name;
            car.ImageUrl = GetString(vehicle, "image");
            car.Type = GetString(vehicle, "type");
            car.PowerHp = GetInt(vehicle, "power");
            car.WeightKg = GetInt(vehicle, "weight");
            car.PowerToWeight = GetDecimal(vehicle, "powerToWeight");
            car.DriveType = GetString(vehicle, "drive");
            car.EngineType = GetString(vehicle, "engineType");
            car.EngineModel = GetString(vehicle, "engineModel");
            car.EngineVolumeCm3 = GetInt(vehicle, "engineVolume");
            car.TorqueNm = GetInt(vehicle, "torque");

            await dbContext.SaveChangesAsync(cancellationToken);
            result.Cars++;
        }
    }

    private async Task ImportChampionshipsAsync(
        JsonElement userJson,
        UserEntity user,
        IReadOnlyDictionary<string, int> disciplineIds,
        JsonImportResult result,
        CancellationToken cancellationToken)
    {
        if (!userJson.TryGetProperty("championships", out var championships) || championships.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var driver = await dbContext.Drivers.FirstOrDefaultAsync(x => x.UserId == user.UserId, cancellationToken);
        var organizer = await dbContext.Users.OrderBy(x => x.UserId).FirstOrDefaultAsync(x => x.Role!.Name == "Organizer", cancellationToken)
            ?? await dbContext.Users.OrderBy(x => x.UserId).FirstAsync(cancellationToken);

        foreach (var championshipJson in championships.EnumerateArray())
        {
            var name = GetString(championshipJson, "name");
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var year = GetInt(championshipJson, "season") ?? DateTime.UtcNow.Year;
            var disciplineName = MapDiscipline(GetString(championshipJson, "discipline"));
            var disciplineId = disciplineIds.TryGetValue(disciplineName, out var foundDisciplineId) ? foundDisciplineId : disciplineIds.Values.First();

            var championship = await dbContext.Championships.FirstOrDefaultAsync(x => x.Name == name && x.SeasonYear == year, cancellationToken);
            if (championship is null)
            {
                championship = new ChampionshipEntity
                {
                    OrganizerId = organizer.UserId,
                    DisciplineId = disciplineId,
                    Name = name,
                    SeasonYear = year,
                    Status = MapChampionshipStatus(GetString(championshipJson, "status")),
                    CreatedAt = DateTime.UtcNow
                };
                dbContext.Championships.Add(championship);
                await dbContext.SaveChangesAsync(cancellationToken);
                result.Championships++;
            }

            if (driver is not null)
            {
                var standing = await dbContext.ChampionshipStandings.FirstOrDefaultAsync(x => x.ChampionshipId == championship.ChampionshipId && x.DriverId == driver.DriverId, cancellationToken);
                if (standing is null)
                {
                    standing = new ChampionshipStandingEntity
                    {
                        ChampionshipId = championship.ChampionshipId,
                        DriverId = driver.DriverId
                    };
                    dbContext.ChampionshipStandings.Add(standing);
                }

                standing.Position = GetInt(championshipJson, "position") ?? 999;
                standing.TotalPoints = GetDecimal(championshipJson, "points") ?? 0;
                await dbContext.SaveChangesAsync(cancellationToken);
                result.ChampionshipStandings++;
            }
        }
    }

    private async Task ImportSupportTicketsAsync(
        string path,
        IReadOnlyDictionary<string, UserEntity> usersByExternalId,
        JsonImportResult result,
        CancellationToken cancellationToken)
    {
        using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(path, cancellationToken));
        if (!doc.RootElement.TryGetProperty("tickets", out var tickets) || tickets.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var fallbackAdmin = await dbContext.Users.OrderBy(x => x.UserId).FirstOrDefaultAsync(x => x.Role!.Name == "TechnicalAdmin", cancellationToken)
            ?? await dbContext.Users.OrderBy(x => x.UserId).FirstAsync(cancellationToken);

        foreach (var ticketJson in tickets.EnumerateArray())
        {
            var externalId = GetString(ticketJson, "id");
            var userExternalId = GetString(ticketJson, "userId");
            var senderEmail = GetString(ticketJson, "email");
            UserEntity? user = null;
            if (!string.IsNullOrWhiteSpace(userExternalId))
            {
                usersByExternalId.TryGetValue(userExternalId, out user);
                user ??= await dbContext.Users.FirstOrDefaultAsync(x => x.ExternalId == userExternalId, cancellationToken);
            }
            user ??= await dbContext.Users.FirstOrDefaultAsync(x => x.Email == senderEmail, cancellationToken);

            var ticket = await dbContext.SupportTickets.FirstOrDefaultAsync(x => x.ExternalId == externalId, cancellationToken);
            if (ticket is null)
            {
                ticket = new SupportTicketEntity { CreatedAt = GetDate(ticketJson, "createdAtUtc") ?? DateTime.UtcNow };
                dbContext.SupportTickets.Add(ticket);
            }

            ticket.ExternalId = externalId;
            ticket.UserId = user?.UserId;
            ticket.SenderName = GetString(ticketJson, "name");
            ticket.SenderEmail = senderEmail;
            ticket.Subject = GetString(ticketJson, "subject");
            ticket.Category = GetString(ticketJson, "category");
            ticket.Message = GetString(ticketJson, "message");
            ticket.Status = MapTicketStatus(GetString(ticketJson, "status"));
            ticket.UpdatedAt = GetDate(ticketJson, "updatedAtUtc");
            await dbContext.SaveChangesAsync(cancellationToken);
            result.SupportTickets++;

            if (!ticketJson.TryGetProperty("answers", out var answers) || answers.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var answerJson in answers.EnumerateArray())
            {
                var answerExternalId = GetString(answerJson, "id");
                var adminExternalId = GetString(answerJson, "adminUserId");
                var admin = !string.IsNullOrWhiteSpace(adminExternalId)
                    ? await dbContext.Users.FirstOrDefaultAsync(x => x.ExternalId == adminExternalId, cancellationToken)
                    : null;
                admin ??= fallbackAdmin;

                var answer = await dbContext.SupportMessages.FirstOrDefaultAsync(x => x.ExternalId == answerExternalId, cancellationToken);
                if (answer is null)
                {
                    answer = new SupportMessageEntity { CreatedAt = GetDate(answerJson, "createdAtUtc") ?? DateTime.UtcNow };
                    dbContext.SupportMessages.Add(answer);
                }

                answer.ExternalId = answerExternalId;
                answer.SupportTicketId = ticket.SupportTicketId;
                answer.AdminUserId = admin.UserId;
                answer.Message = GetString(answerJson, "message");
                answer.EmailHtml = GetString(answerJson, "emailHtml");
                answer.EmailDeliveryStatus = GetString(answerJson, "emailDeliveryStatus");
                answer.EmailDeliveryError = GetString(answerJson, "emailDeliveryError");
                await dbContext.SaveChangesAsync(cancellationToken);
                result.SupportMessages++;
            }
        }
    }

    private async Task<TeamEntity> EnsureTeamAsync(string name, string? logoUrl, string? color, CancellationToken cancellationToken)
    {
        var team = await dbContext.Teams.FirstOrDefaultAsync(x => x.Name == name, cancellationToken);
        if (team is not null)
        {
            if (!string.IsNullOrWhiteSpace(logoUrl)) team.LogoUrl = logoUrl;
            if (!string.IsNullOrWhiteSpace(color)) team.AccentColor = color;
            await dbContext.SaveChangesAsync(cancellationToken);
            return team;
        }

        team = new TeamEntity
        {
            Name = name,
            LogoUrl = logoUrl,
            AccentColor = color,
            Description = "Команда RaceManager",
            CreatedAt = DateTime.UtcNow
        };
        dbContext.Teams.Add(team);
        await dbContext.SaveChangesAsync(cancellationToken);
        return team;
    }

    private static string MapRole(string role) => role.Trim() switch
    {
        "Организатор" => "Organizer",
        "Судья" => "Judge",
        "Технический админ" => "TechnicalAdmin",
        "Технический администратор" => "TechnicalAdmin",
        _ => "User"
    };

    private static string MapDiscipline(string discipline) => discipline.Trim() switch
    {
        "Дрэг-рейсинг" => "DragRacing",
        "Дрэг" => "DragRacing",
        "Тайм-Аттак" => "TimeAttack",
        "Time-Attack" => "TimeAttack",
        _ => "Drift"
    };

    private static string MapTicketStatus(string status) => status.Trim() switch
    {
        "Рассмотренное" => "Reviewed",
        "Отклонено" => "Rejected",
        _ => "Waiting"
    };

    private static string MapChampionshipStatus(string status) => status.Trim() switch
    {
        "Завершён" => "Completed",
        "Завершен" => "Completed",
        "Активный сезон" => "Active",
        _ => "Published"
    };

    private static string NormalizeTeamName(string name) => name.Trim() switch
    {
        "Betera Drift" => "Betera",
        "LowBudgetDrift" => "Low Budget Drift",
        "Blockchain Sport" => "Blockchain Sports",
        _ => name.Trim()
    };

    private static string GetString(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Undefined || element.ValueKind == JsonValueKind.Null)
        {
            return string.Empty;
        }

        if (!element.TryGetProperty(propertyName, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return string.Empty;
        }

        return value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : value.ToString();
    }

    private static int? GetInt(JsonElement element, string propertyName)
    {
        var value = GetString(element, propertyName);
        return int.TryParse(value, out var result) ? result : null;
    }

    private static decimal? GetDecimal(JsonElement element, string propertyName)
    {
        var value = GetString(element, propertyName);
        return decimal.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }

    private static DateTime? GetDate(JsonElement element, string propertyName)
    {
        var value = GetString(element, propertyName);
        return DateTime.TryParse(value, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out var result)
            ? result
            : null;
    }
}
