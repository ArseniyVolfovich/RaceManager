using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using RaceManager.Application.Interfaces;
using RaceManager.Domain.Entities;
using RaceManager.Infrastructure.Database.Models;

namespace RaceManager.Infrastructure.Database;

public sealed class SqlUserRepository(RaceManagerDbContext dbContext) : IUserRepository
{
    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await QueryUsers().OrderBy(x => x.Login).ToListAsync(cancellationToken);
        return entities.Select(ToDomain).ToList();
    }

    public async Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var entity = await QueryUsers().FirstOrDefaultAsync(x => x.ExternalId == id || x.UserId.ToString() == id, cancellationToken);
        return entity is null ? null : ToDomain(entity);
    }

    public async Task<User?> FindByEmailOrLoginAsync(string emailOrLogin, CancellationToken cancellationToken = default)
    {
        var value = emailOrLogin.Trim();
        var entity = await QueryUsers().FirstOrDefaultAsync(x => x.Email == value || x.Login == value, cancellationToken);
        return entity is null ? null : ToDomain(entity);
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        var value = email.Trim();
        return dbContext.Users.AnyAsync(x => x.Email == value, cancellationToken);
    }

    public Task<bool> LoginExistsAsync(string login, CancellationToken cancellationToken = default)
    {
        var value = login.Trim();
        return dbContext.Users.AnyAsync(x => x.Login == value, cancellationToken);
    }

    public Task<bool> PhoneExistsAsync(string phone, CancellationToken cancellationToken = default)
    {
        var value = NormalizePhone(phone);
        return dbContext.Users.AnyAsync(x => x.Phone != null && x.Phone.Replace(" ", "").Replace("-", "") == value, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        var role = await GetOrCreateRoleAsync(ToRoleName(user.Role), cancellationToken);
        var entity = new UserEntity
        {
            ExternalId = string.IsNullOrWhiteSpace(user.Id) ? $"user-{Guid.NewGuid():N}"[..13] : user.Id,
            RoleId = role.RoleId,
            Login = user.Login.Trim(),
            Email = user.Email.Trim(),
            PasswordHash = user.Password,
            LastName = user.Profile.LastName,
            FirstName = user.Profile.FirstName,
            MiddleName = user.Profile.MiddleName,
            Phone = user.Profile.Phone,
            AvatarUrl = user.Avatar,
            OrganizationName = user.Profile.OrganizationName,
            OrganizationColor = user.Profile.OrganizationColor,
            OrganizationLogoUrl = user.Profile.OrganizationLogo,
            OrganizationBannerUrl = user.Profile.OrganizationBanner,
            OrganizationMembersJson = JsonSerializer.Serialize(user.Profile.OrganizationMembers),
            RacingTeamName = user.Profile.RacingTeamName,
            RacingTeamColor = user.Profile.RacingTeamColor,
            RacingTeamLogoUrl = user.Profile.RacingTeamLogo,
            RacingTeamBannerUrl = user.Profile.RacingTeamBanner,
            RacingTeamMembersJson = JsonSerializer.Serialize(user.Profile.RacingTeamMembers),
            TeamInvitationsJson = JsonSerializer.Serialize(user.Profile.TeamInvitations),
            TeamNotificationsJson = JsonSerializer.Serialize(user.Profile.TeamNotifications),
            TeamMembershipsJson = JsonSerializer.Serialize(user.Profile.TeamMemberships),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };

        dbContext.Users.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (ToRoleName(user.Role) == "User")
        {
            dbContext.Drivers.Add(new DriverEntity
            {
                UserId = entity.UserId,
                DriverNumber = ReadDriverNumber(user),
                TotalPoints = user.Statistics.Points,
                RacesCount = user.Statistics.Races,
                WinsCount = user.Statistics.Wins,
                PodiumsCount = user.Statistics.Podiums,
                RatingPosition = user.Statistics.Ranking
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await SyncCarsAsync(entity, user, cancellationToken);
        user.Id = entity.ExternalId ?? entity.UserId.ToString();
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        var entity = await QueryUsers().FirstOrDefaultAsync(x => x.ExternalId == user.Id || x.UserId.ToString() == user.Id, cancellationToken);
        if (entity is null)
        {
            await AddAsync(user, cancellationToken);
            return;
        }

        var role = await GetOrCreateRoleAsync(ToRoleName(user.Role), cancellationToken);
        entity.RoleId = role.RoleId;
        entity.Login = user.Login.Trim();
        entity.Email = user.Email.Trim();
        if (!string.IsNullOrWhiteSpace(user.Password)) entity.PasswordHash = user.Password;
        entity.LastName = user.Profile.LastName;
        entity.FirstName = user.Profile.FirstName;
        entity.MiddleName = user.Profile.MiddleName;
        entity.Phone = user.Profile.Phone;
        entity.AvatarUrl = user.Avatar;
        entity.OrganizationName = user.Profile.OrganizationName;
        entity.OrganizationColor = user.Profile.OrganizationColor;
        entity.OrganizationLogoUrl = user.Profile.OrganizationLogo;
        entity.OrganizationBannerUrl = user.Profile.OrganizationBanner;
        entity.OrganizationMembersJson = JsonSerializer.Serialize(user.Profile.OrganizationMembers);
        entity.RacingTeamName = user.Profile.RacingTeamName;
        entity.RacingTeamColor = user.Profile.RacingTeamColor;
        entity.RacingTeamLogoUrl = user.Profile.RacingTeamLogo;
        entity.RacingTeamBannerUrl = user.Profile.RacingTeamBanner;
        entity.RacingTeamMembersJson = JsonSerializer.Serialize(user.Profile.RacingTeamMembers);
        entity.TeamInvitationsJson = JsonSerializer.Serialize(user.Profile.TeamInvitations);
        entity.TeamNotificationsJson = JsonSerializer.Serialize(user.Profile.TeamNotifications);
        entity.TeamMembershipsJson = JsonSerializer.Serialize(user.Profile.TeamMemberships);
        entity.UpdatedAt = DateTime.UtcNow;
        entity.IsActive = true;

        if (entity.Driver is null && ToRoleName(user.Role) == "User")
        {
            entity.Driver = new DriverEntity { UserId = entity.UserId };
        }
        if (entity.Driver is not null)
        {
            entity.Driver.DriverNumber = ReadDriverNumber(user);
            entity.Driver.TotalPoints = user.Statistics.Points;
            entity.Driver.RacesCount = user.Statistics.Races;
            entity.Driver.WinsCount = user.Statistics.Wins;
            entity.Driver.PodiumsCount = user.Statistics.Podiums;
            entity.Driver.RatingPosition = user.Statistics.Ranking;
        }

        await SyncCarsAsync(entity, user, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<UserEntity> QueryUsers()
    {
        return dbContext.Users
            .Include(x => x.Role)
            .Include(x => x.Driver).ThenInclude(x => x!.Team)
            .Include(x => x.Cars)
            .Include(x => x.Registrations).ThenInclude(x => x.Event).ThenInclude(x => x!.Discipline)
            .AsSplitQuery();
    }

    private async Task<RoleEntity> GetOrCreateRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        var role = await dbContext.Roles.FirstOrDefaultAsync(x => x.Name == roleName, cancellationToken);
        if (role is not null) return role;

        role = new RoleEntity { Name = roleName, DisplayName = ToDisplayRole(roleName) };
        dbContext.Roles.Add(role);
        await dbContext.SaveChangesAsync(cancellationToken);
        return role;
    }

    private async Task SyncCarsAsync(UserEntity entity, User user, CancellationToken cancellationToken)
    {
        var wantedIds = user.Vehicles.Select(x => x.Id).Where(x => !string.IsNullOrWhiteSpace(x)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var car in entity.Cars.Where(x => !string.IsNullOrWhiteSpace(x.ExternalId) && !wantedIds.Contains(x.ExternalId!)).ToList())
        {
            dbContext.Cars.Remove(car);
        }

        foreach (var vehicle in user.Vehicles)
        {
            var car = entity.Cars.FirstOrDefault(x => x.ExternalId == vehicle.Id);
            if (car is null)
            {
                car = new CarEntity
                {
                    ExternalId = string.IsNullOrWhiteSpace(vehicle.Id) ? $"vehicle-{Guid.NewGuid():N}"[..16] : vehicle.Id,
                    UserId = entity.UserId,
                    CreatedAt = DateTime.UtcNow
                };
                entity.Cars.Add(car);
            }

            car.Name = vehicle.Name;
            car.ImageUrl = vehicle.Image;
            car.Type = GetSpec(vehicle, "Тип");
            car.PowerHp = ToNullableInt(GetSpec(vehicle, "Мощность л.с."));
            car.WeightKg = ToNullableInt(GetSpec(vehicle, "Вес, кг"));
            car.PowerToWeight = ToNullableDecimal(GetSpec(vehicle, "Удельная мощность, л.с./т."));
            car.DriveType = GetSpec(vehicle, "Привод");
            car.EngineType = GetSpec(vehicle, "Тип двигателя");
            car.EngineModel = GetSpec(vehicle, "Модель двигателя");
            car.EngineVolumeCm3 = ToNullableInt(GetSpec(vehicle, "Объем, см3"));
            car.TorqueNm = ToNullableInt(GetSpec(vehicle, "Крутящий момент, Нм"));
            car.IsTeamVehicle = vehicle.IsTeamVehicle;
            car.TeamName = vehicle.TeamName;
            car.TeamLogoUrl = vehicle.TeamLogo;
        }

        await Task.CompletedTask;
    }

    private static User ToDomain(UserEntity entity)
    {
        var role = ToDisplayRole(entity.Role?.Name ?? "User");
        var user = new User
        {
            Id = entity.ExternalId ?? entity.UserId.ToString(),
            Login = entity.Login,
            Email = entity.Email,
            Password = entity.PasswordHash,
            Role = role,
            Avatar = entity.AvatarUrl ?? string.Empty,
            Profile = new UserProfile
            {
                LastName = entity.LastName ?? string.Empty,
                FirstName = entity.FirstName ?? string.Empty,
                MiddleName = entity.MiddleName ?? string.Empty,
                Phone = entity.Phone ?? string.Empty,
                Car = entity.Cars.FirstOrDefault(x => x.IsFavorite)?.Name ?? entity.Cars.FirstOrDefault()?.Name ?? string.Empty,
                OrganizationName = entity.OrganizationName ?? string.Empty,
                OrganizationColor = entity.OrganizationColor ?? "#e10600",
                OrganizationLogo = entity.OrganizationLogoUrl ?? string.Empty,
                OrganizationBanner = entity.OrganizationBannerUrl ?? string.Empty,
                OrganizationMembers = DeserializeOrganizationMembers(entity.OrganizationMembersJson),
                RacingTeamName = entity.RacingTeamName ?? string.Empty,
                RacingTeamColor = entity.RacingTeamColor ?? "#e10600",
                RacingTeamLogo = entity.RacingTeamLogoUrl ?? string.Empty,
                RacingTeamBanner = entity.RacingTeamBannerUrl ?? string.Empty,
                RacingTeamMembers = DeserializeOrganizationMembers(entity.RacingTeamMembersJson),
                TeamInvitations = DeserializeList<TeamInvitation>(entity.TeamInvitationsJson),
                TeamNotifications = DeserializeList<TeamNotification>(entity.TeamNotificationsJson),
                TeamMemberships = DeserializeList<TeamMembership>(entity.TeamMembershipsJson),
                DriverNumber = entity.Driver?.DriverNumber ?? string.Empty
            },
            Statistics = new UserStatistics
            {
                Ranking = entity.Driver?.RatingPosition ?? 1,
                Points = (int)(entity.Driver?.TotalPoints ?? 0),
                Races = entity.Driver?.RacesCount ?? 0,
                Wins = entity.Driver?.WinsCount ?? 0,
                Podiums = entity.Driver?.PodiumsCount ?? 0
            },
            Vehicles = entity.Cars.Select(ToVehicle).ToList(),
            Applications = entity.Registrations.Select(ToApplication).ToList()
        };

        if (entity.Driver?.Team is not null)
        {
            user.Team = new TeamInfo
            {
                Name = entity.Driver.Team.Name,
                Logo = entity.Driver.Team.LogoUrl ?? string.Empty,
                Color = entity.Driver.Team.AccentColor ?? string.Empty,
                Role = "Пилот"
            };
        }

        return user;
    }

    private static Vehicle ToVehicle(CarEntity car)
    {
        return new Vehicle
        {
            Id = car.ExternalId ?? car.CarId.ToString(),
            Name = car.Name,
            Image = car.ImageUrl ?? string.Empty,
            IsTeamVehicle = car.IsTeamVehicle,
            TeamName = car.TeamName ?? string.Empty,
            TeamLogo = car.TeamLogoUrl ?? string.Empty,
            Specs = new Dictionary<string, string>
            {
                ["Тип"] = car.Type ?? "Не указано",
                ["Мощность л.с."] = car.PowerHp?.ToString() ?? "Не указано",
                ["Вес, кг"] = car.WeightKg?.ToString() ?? "Не указано",
                ["Удельная мощность, л.с./т."] = car.PowerToWeight?.ToString("0.##") ?? "Не указано",
                ["Привод"] = car.DriveType ?? "Не указано",
                ["Тип двигателя"] = car.EngineType ?? "Не указано",
                ["Модель двигателя"] = car.EngineModel ?? "Не указано",
                ["Объем, см3"] = car.EngineVolumeCm3?.ToString() ?? "Не указано",
                ["Крутящий момент, Нм"] = car.TorqueNm?.ToString() ?? "Не указано"
            }
        };
    }

    private static UserApplication ToApplication(RegistrationEntity registration)
    {
        return new UserApplication
        {
            Id = registration.RegistrationId.ToString(),
            EventName = registration.Event?.Name ?? "Событие",
            Date = registration.Event?.DateStart.ToString("dd.MM.yyyy") ?? string.Empty,
            Location = registration.Event?.Track?.Location ?? string.Empty,
            Discipline = registration.Event?.Discipline?.DisplayName ?? string.Empty,
            Status = registration.Status switch
            {
                "Withdrawn" => "Отклонил участие",
                "Approved" => "Подтверждено",
                "Declined" => "Отклонено",
                _ => "На рассмотрении"
            }
        };
    }

    private static string ToRoleName(string role)
    {
        return role.Trim().ToLowerInvariant() switch
        {
            "организатор" or "organizer" => "Organizer",
            "судья" or "judge" => "Judge",
            "технический администратор" or "тех администратор" or "technicaladmin" => "TechnicalAdmin",
            _ => "User"
        };
    }

    private static string ToDisplayRole(string roleName)
    {
        return roleName switch
        {
            "Organizer" => "Организатор",
            "Judge" => "Судья",
            "TechnicalAdmin" => "Технический администратор",
            _ => "Пользователь"
        };
    }

    private static string GetSpec(Vehicle vehicle, string key)
    {
        return vehicle.Specs.TryGetValue(key, out var value) && value != "Не указано" ? value : string.Empty;
    }

    private static int? ToNullableInt(string value)
    {
        return int.TryParse(value, out var result) ? result : null;
    }

    private static decimal? ToNullableDecimal(string value)
    {
        return decimal.TryParse(value.Replace(',', '.'), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var result) ? result : null;
    }

    private static List<OrganizationMember> DeserializeOrganizationMembers(string? json) => DeserializeList<OrganizationMember>(json);

    private static List<T> DeserializeList<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<List<T>>(json) ?? []; }
        catch (JsonException) { return []; }
    }

    private static string NormalizePhone(string phone)
    {
        return phone.Replace(" ", string.Empty).Replace("-", string.Empty);
    }

    private static string ReadDriverNumber(User user)
    {
        var property = user.Profile.GetType().GetProperty("DriverNumber");
        return property?.GetValue(user.Profile)?.ToString() ?? string.Empty;
    }
}
