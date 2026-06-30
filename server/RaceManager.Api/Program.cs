using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using RaceManager.Application.DTOs;
using RaceManager.Application.Interfaces;
using RaceManager.Application.Services;
using RaceManager.Application.Security;
using RaceManager.Domain.Entities;
using RaceManager.Infrastructure.JsonStorage;
using RaceManager.Infrastructure.Email;
using RaceManager.Infrastructure.Database;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: true, reloadOnChange: true)
    .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.Development.json"), optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

var contentRoot = builder.Environment.ContentRootPath;
var clientCandidates = new[]
{
    Path.Combine(contentRoot, "client"),
    Path.GetFullPath(Path.Combine(contentRoot, "..", "client")),
    Path.GetFullPath(Path.Combine(contentRoot, "..", "..", "client")),
    Path.GetFullPath(Path.Combine(contentRoot, "..", "..", "..", "client")),
    Path.GetFullPath(Path.Combine(contentRoot, "..", "..", "..", "..", "client")),
    Path.GetFullPath(Path.Combine(contentRoot, "..", "..", "..", "..", "..", "client"))
};
var clientPath = clientCandidates.FirstOrDefault(path => File.Exists(Path.Combine(path, "index.html")));

var dataCandidates = new[]
{
    Path.Combine(contentRoot, "server", "data"),
    Path.Combine(contentRoot, "data"),
    Path.GetFullPath(Path.Combine(contentRoot, "..", "data")),
    Path.GetFullPath(Path.Combine(contentRoot, "..", "..", "data")),
    Path.GetFullPath(Path.Combine(contentRoot, "..", "..", "..", "data")),
    Path.GetFullPath(Path.Combine(contentRoot, "..", "..", "..", "..", "data"))
};
var dataPath = dataCandidates.FirstOrDefault(path => File.Exists(Path.Combine(path, "users.json")))
    ?? dataCandidates.First(path => path.EndsWith($"{Path.DirectorySeparatorChar}data", StringComparison.Ordinal));
var usersPath = Path.Combine(dataPath, "users.json");
var eventsPath = Path.Combine(dataPath, "events.json");
var resultsPath = Path.Combine(dataPath, "results.json");
var supportTicketsPath = Path.Combine(dataPath, "support-tickets.json");
var smtpOptions = new SmtpEmailOptions
{
    Host = builder.Configuration["Email:Smtp:Host"] ?? string.Empty,
    Port = builder.Configuration.GetValue("Email:Smtp:Port", 587),
    EnableSsl = builder.Configuration.GetValue("Email:Smtp:EnableSsl", true),
    UserName = builder.Configuration["Email:Smtp:UserName"] ?? string.Empty,
    Password = builder.Configuration["Email:Smtp:Password"] ?? string.Empty,
    FromEmail = builder.Configuration["Email:Smtp:FromEmail"] ?? string.Empty,
    FromName = builder.Configuration["Email:Smtp:FromName"] ?? "RaceManager ID"
};

builder.Services.AddSingleton(new JsonStorageOptions
{
    UsersPath = usersPath,
    EventsPath = eventsPath,
    ResultsPath = resultsPath,
    SupportTicketsPath = supportTicketsPath
});
builder.Services.AddScoped<IUserRepository, JsonUserRepository>();
builder.Services.AddScoped<IEventRepository, JsonEventRepository>();
builder.Services.AddScoped<IResultRepository, JsonResultRepository>();
builder.Services.AddScoped<ISupportTicketRepository, JsonSupportTicketRepository>();
builder.Services.AddSingleton(smtpOptions);
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserProfileService>();
builder.Services.AddScoped<EventService>();
builder.Services.AddScoped<JudgeResultService>();
builder.Services.AddScoped<EventJudgeService>();
builder.Services.AddScoped<SupportService>();
builder.Services.AddScoped<StartListService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<ChampionshipService>();
builder.Services.AddRaceManagerSqlServer(builder.Configuration);
builder.Services.AddScoped<JsonToSqlImportService>();

builder.Services.AddControllers();
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "RaceManager.Session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendDev", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5500",
                "http://127.0.0.1:5500",
                "http://localhost:5173",
                "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

try
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetService<RaceManagerDbContext>();
    if (dbContext is not null && dbContext.Database.IsSqlServer())
    {
        await dbContext.Database.ExecuteSqlRawAsync("""
            IF EXISTS (
                SELECT 1
                FROM sys.columns
                WHERE object_id = OBJECT_ID(N'dbo.Championships')
                  AND name = N'BannerUrl'
                  AND max_length <> -1
            )
                ALTER TABLE dbo.Championships ALTER COLUMN BannerUrl NVARCHAR(MAX) NULL;

            IF EXISTS (
                SELECT 1
                FROM sys.columns
                WHERE object_id = OBJECT_ID(N'dbo.Events')
                  AND name = N'BannerUrl'
                  AND max_length <> -1
            )
                ALTER TABLE dbo.Events ALTER COLUMN BannerUrl NVARCHAR(MAX) NULL;

            IF EXISTS (
                SELECT 1
                FROM sys.columns
                WHERE object_id = OBJECT_ID(N'dbo.Events')
                  AND name = N'TrackConfigImageUrl'
                  AND max_length <> -1
            )
                ALTER TABLE dbo.Events ALTER COLUMN TrackConfigImageUrl NVARCHAR(MAX) NULL;

            IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Users') AND name = N'AvatarUrl' AND max_length <> -1)
                ALTER TABLE dbo.Users ALTER COLUMN AvatarUrl NVARCHAR(MAX) NULL;

            IF COL_LENGTH('dbo.Users', 'OrganizationName') IS NULL ALTER TABLE dbo.Users ADD OrganizationName NVARCHAR(160) NULL;
            IF COL_LENGTH('dbo.Users', 'OrganizationColor') IS NULL ALTER TABLE dbo.Users ADD OrganizationColor NVARCHAR(20) NULL;
            IF COL_LENGTH('dbo.Users', 'OrganizationLogoUrl') IS NULL ALTER TABLE dbo.Users ADD OrganizationLogoUrl NVARCHAR(MAX) NULL;
            IF COL_LENGTH('dbo.Users', 'OrganizationBannerUrl') IS NULL ALTER TABLE dbo.Users ADD OrganizationBannerUrl NVARCHAR(MAX) NULL;
            IF COL_LENGTH('dbo.Users', 'OrganizationMembersJson') IS NULL ALTER TABLE dbo.Users ADD OrganizationMembersJson NVARCHAR(MAX) NULL;
            IF COL_LENGTH('dbo.Users', 'RacingTeamName') IS NULL ALTER TABLE dbo.Users ADD RacingTeamName NVARCHAR(160) NULL;
            IF COL_LENGTH('dbo.Users', 'RacingTeamColor') IS NULL ALTER TABLE dbo.Users ADD RacingTeamColor NVARCHAR(20) NULL;
            IF COL_LENGTH('dbo.Users', 'RacingTeamLogoUrl') IS NULL ALTER TABLE dbo.Users ADD RacingTeamLogoUrl NVARCHAR(MAX) NULL;
            IF COL_LENGTH('dbo.Users', 'RacingTeamBannerUrl') IS NULL ALTER TABLE dbo.Users ADD RacingTeamBannerUrl NVARCHAR(MAX) NULL;
            IF COL_LENGTH('dbo.Users', 'RacingTeamMembersJson') IS NULL ALTER TABLE dbo.Users ADD RacingTeamMembersJson NVARCHAR(MAX) NULL;
            IF COL_LENGTH('dbo.Users', 'TeamInvitationsJson') IS NULL ALTER TABLE dbo.Users ADD TeamInvitationsJson NVARCHAR(MAX) NULL;
            IF COL_LENGTH('dbo.Users', 'TeamNotificationsJson') IS NULL ALTER TABLE dbo.Users ADD TeamNotificationsJson NVARCHAR(MAX) NULL;
            IF COL_LENGTH('dbo.Users', 'TeamMembershipsJson') IS NULL ALTER TABLE dbo.Users ADD TeamMembershipsJson NVARCHAR(MAX) NULL;

            IF COL_LENGTH('dbo.Registrations', 'TeamName') IS NULL ALTER TABLE dbo.Registrations ADD TeamName NVARCHAR(160) NULL;

            IF COL_LENGTH('dbo.Cars', 'IsTeamVehicle') IS NULL ALTER TABLE dbo.Cars ADD IsTeamVehicle BIT NOT NULL CONSTRAINT DF_Cars_IsTeamVehicle_Auto DEFAULT 0;
            IF COL_LENGTH('dbo.Cars', 'TeamName') IS NULL ALTER TABLE dbo.Cars ADD TeamName NVARCHAR(160) NULL;
            IF COL_LENGTH('dbo.Cars', 'TeamLogoUrl') IS NULL ALTER TABLE dbo.Cars ADD TeamLogoUrl NVARCHAR(MAX) NULL;
            IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Cars') AND name = N'ImageUrl' AND max_length <> -1)
                ALTER TABLE dbo.Cars ALTER COLUMN ImageUrl NVARCHAR(MAX) NULL;

            IF COL_LENGTH('dbo.Events', 'CalendarBannerUrl') IS NULL ALTER TABLE dbo.Events ADD CalendarBannerUrl NVARCHAR(MAX) NULL;
            IF COL_LENGTH('dbo.Events', 'OrganizerName') IS NULL ALTER TABLE dbo.Events ADD OrganizerName NVARCHAR(160) NULL;
            IF COL_LENGTH('dbo.Events', 'OrganizerColor') IS NULL ALTER TABLE dbo.Events ADD OrganizerColor NVARCHAR(20) NULL;
            IF COL_LENGTH('dbo.Events', 'OrganizerLogoUrl') IS NULL ALTER TABLE dbo.Events ADD OrganizerLogoUrl NVARCHAR(MAX) NULL;
            IF COL_LENGTH('dbo.Events', 'StagesJson') IS NULL ALTER TABLE dbo.Events ADD StagesJson NVARCHAR(MAX) NULL;
            """);
    }
}
catch (Exception error)
{
    app.Logger.LogWarning(error, "Не удалось автоматически обновить размер полей изображений событий.");
}

try
{
    await using var scope = app.Services.CreateAsyncScope();
    var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
    var standardOrganizers = new[]
    {
        new User
        {
            Id = "organizer-004",
            Login = "event.organizer",
            Email = "event.organizer@racemanager.test",
            Password = PasswordSecurity.Hash("Organizer2026!"),
            Role = "Организатор",
            Profile = new UserProfile
            {
                LastName = "Орлова",
                FirstName = "Марина",
                MiddleName = "Андреевна",
                Phone = "+375 33 555-44-55",
                BirthDate = "1990-06-15"
            }
        },
        new User
        {
            Id = "organizer-standard",
            Login = "standard.organizer",
            Email = "standard.organizer@racemanager.test",
            Password = PasswordSecurity.Hash("OrganizerStandard2026!"),
            Role = "Организатор",
            Profile = new UserProfile
            {
                LastName = "Смирнов",
                FirstName = "Илья",
                MiddleName = "Павлович",
                Phone = "+375 29 330-44-55",
                BirthDate = "1991-03-22"
            }
        },
        new User
        {
            Id = "organizer-22rt",
            Login = "22rt.organizer",
            Email = "22rt@gmail.com",
            Password = PasswordSecurity.Hash("Login123"),
            Role = "Организатор",
            Profile = new UserProfile
            {
                LastName = "22RT",
                FirstName = "Организатор",
                MiddleName = "",
                Phone = "+375 29 220-22-22",
                BirthDate = "1992-02-22"
            }
        }
    };

    foreach (var account in standardOrganizers)
    {
        if (await users.FindByEmailOrLoginAsync(account.Email) is null && await users.FindByEmailOrLoginAsync(account.Login) is null)
        {
            await users.AddAsync(account);
        }
    }
}
catch (Exception error)
{
    app.Logger.LogWarning(error, "Не удалось создать демонстрационную учетную запись организатора.");
}


try
{
    await using var scope = app.Services.CreateAsyncScope();
    var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
    var demoAccounts = new[]
    {
        new User
        {
            Id = DemoAccess.GlobalOrganizerUserId,
            Login = "organizer.global",
            Email = "organizer.global@racemanager.test",
            Password = PasswordSecurity.Hash("OrganizerGlobal2026!"),
            Role = "Организатор",
            Profile = new UserProfile
            {
                LastName = "Глобальный",
                FirstName = "Организатор",
                Phone = "+375 29 220-00-01",
                OrganizationName = "22RT",
                OrganizationColor = "#ed1c24",
                OrganizationLogo = "/public/22RT.png",
                OrganizationBanner = "/public/22RTtimeattack.png"
            }
        },
        new User
        {
            Id = DemoAccess.GlobalJudgeUserId,
            Login = "judge.global",
            Email = "judge.global@racemanager.test",
            Password = PasswordSecurity.Hash("JudgeGlobal2026!"),
            Role = "Судья",
            Profile = new UserProfile
            {
                LastName = "Глобальный",
                FirstName = "Судья",
                Phone = "+375 29 220-00-02"
            }
        }
    };

    foreach (var account in demoAccounts)
    {
        var existing = await users.FindByEmailOrLoginAsync(account.Email);
        if (existing is null)
        {
            await users.AddAsync(account);
            continue;
        }

        existing.Password = account.Password;
        existing.Role = account.Role;
        existing.Profile.LastName = account.Profile.LastName;
        existing.Profile.FirstName = account.Profile.FirstName;
        existing.Profile.Phone = account.Profile.Phone;
        existing.Profile.OrganizationName = account.Profile.OrganizationName;
        existing.Profile.OrganizationColor = account.Profile.OrganizationColor;
        existing.Profile.OrganizationLogo = account.Profile.OrganizationLogo;
        existing.Profile.OrganizationBanner = account.Profile.OrganizationBanner;
        await users.UpdateAsync(existing);
    }

    const string demoEventId = "event-22rt-demo";
    const string demoSeedMarker = "[22RT-DEMO-V3]";
    var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
    var demoEvent = await eventRepository.GetByIdAsync(demoEventId);
    if (demoEvent is null)
    {
        var driverNames = new[]
        {
            "Алексей Морозов", "Илья Соколов", "Максим Волков", "Денис Орлов",
            "Кирилл Новиков", "Артём Козлов", "Роман Павлов", "Никита Лебедев",
            "Егор Семёнов", "Дмитрий Фёдоров", "Антон Михайлов", "Сергей Власов",
            "Владислав Комаров", "Павел Титов", "Михаил Белов", "Олег Громов",
            "Андрей Зайцев", "Виктор Назаров", "Степан Крылов", "Руслан Макаров",
            "Глеб Воронов", "Ярослав Ковалёв"
        };
        var cars = new[]
        {
            "Porsche 911 GT3", "BMW M4", "Audi RS3", "Toyota GR Supra", "Honda Civic Type R",
            "Nissan GT-R", "Subaru WRX STI", "Mitsubishi Lancer Evo X", "Mazda MX-5",
            "Volkswagen Golf R", "Mercedes-AMG A45", "Ford Mustang GT"
        };
        var classes = new[] { "Stock", "Street", "Sport", "Charged", "Pro", "Unlim" };
        demoEvent = new RaceEvent
        {
            Id = demoEventId,
            OrganizerUserId = DemoAccess.GlobalOrganizerUserId,
            Type = "Трек-день",
            Title = "22RT Time Attack Demo",
            Discipline = "Тайм-Аттак",
            ParticipantLimit = 22,
            Track = "Автодром Стайки",
            Distance = "2500",
            Laps = 3,
            TrackConfigImage = "/public/22RTtimeattack.png",
            BannerImage = "/public/22RTtimeattack.png",
            CalendarBannerImage = "/public/22RTtimeattack.png",
            OrganizerName = "22RT",
            OrganizerColor = "#ed1c24",
            OrganizerLogo = string.Empty,
            Date = "2026-09-12",
            RegistrationStatus = "Регистрация закрыта",
            Intro = $"Демонстрационный трек-день с автоматическим расчётом лучшего круга, штрафов, позиций и очков. {demoSeedMarker}"
        };
        demoEvent.Participants = driverNames.Select((name, index) => new EventParticipant
        {
            Id = $"22rt-pilot-{index + 1:00}",
            UserId = $"22rt-user-{index + 1:00}",
            FullName = name,
            Email = $"22rt.pilot{index + 1:00}@racemanager.test",
            Phone = $"+375 25 2{index + 1:00}-{index + 10:00}-{index + 20:00}",
            Car = cars[index % cars.Length],
            TeamName = index < 11 ? "22RT Red" : "22RT Black",
            DriverNumber = (index + 1).ToString(),
            ClassName = classes[index % classes.Length],
            Status = "Зарегистрирован",
            RegisteredAtUtc = DateTime.UtcNow.AddMinutes(-index)
        }).ToList();

        await eventRepository.AddAsync(demoEvent);
        await eventRepository.UpdateAsync(demoEvent);
    }
    else if (!demoEvent.Intro.Contains(demoSeedMarker, StringComparison.Ordinal))
    {
        var requiresStartListReset = !demoEvent.Intro.Contains("[22RT-DEMO-V2]", StringComparison.Ordinal);
        if (requiresStartListReset) demoEvent.RegistrationStatus = "Регистрация закрыта";
        demoEvent.OrganizerLogo = string.Empty;
        demoEvent.Intro = $"{demoEvent.Intro.Trim()} {demoSeedMarker}".Trim();
        await eventRepository.UpdateAsync(demoEvent);
        if (requiresStartListReset)
            await scope.ServiceProvider.GetRequiredService<StartListService>().ClearAsync(demoEventId);
    }

    var refreshedEvent = await eventRepository.GetByIdAsync(demoEventId);
    if (refreshedEvent is not null)
    {
        var resultService = scope.ServiceProvider.GetRequiredService<JudgeResultService>();
        var existingResults = (await resultService.GetAllAsync())
            .Where(result => result.EventId.Equals(demoEventId, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var random = new Random(220922);
        for (var index = 0; index < refreshedEvent.Participants.Count; index++)
        {
            var participant = refreshedEvent.Participants[index];
            var baseTime = random.Next(54_000, 68_501);
            var lap1 = baseTime + random.Next(0, 1_500);
            var lap2 = baseTime + random.Next(0, 1_500);
            var lap3 = baseTime + random.Next(0, 1_500);
            if (existingResults.Any(result => result.DriverName.Equals(participant.FullName, StringComparison.OrdinalIgnoreCase))) continue;

            await resultService.UpsertAsync(new UpsertRaceResultRequest(
                demoEventId,
                null,
                participant.Id,
                participant.FullName,
                0,
                null,
                null,
                null,
                "Финишировал",
                null,
                lap1,
                lap2,
                lap3,
                0,
                null,
                participant.ClassName,
                participant.Car,
                participant.DriverNumber));
        }
    }
}
catch (Exception error)
{
    app.Logger.LogWarning(error, "Не удалось подготовить демонстрационные аккаунты и событие 22RT.");
}

app.UseCors("FrontendDev");
app.UseAuthentication();
app.UseAuthorization();

if (!string.IsNullOrWhiteSpace(clientPath))
{
    var clientFiles = new PhysicalFileProvider(clientPath);
    app.UseDefaultFiles(new DefaultFilesOptions
    {
        FileProvider = clientFiles,
        DefaultFileNames = new List<string> { "index.html" }
    });
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = clientFiles
    });
}

app.MapControllers();

app.MapGet("/api/status", () => Results.Ok(new
{
    name = "RaceManager API",
    status = "running",
    frontend = clientPath,
    storage = new
    {
        users = usersPath,
        events = eventsPath,
        results = resultsPath,
        supportTickets = supportTicketsPath
    }
}));

if (!string.IsNullOrWhiteSpace(clientPath))
{
    app.MapFallback(async context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.SendFileAsync(Path.Combine(clientPath, "index.html"));
    });
}
else
{
    app.MapGet("/", () => Results.Ok(new
    {
        name = "RaceManager API",
        status = "running",
        frontend = "client folder not found"
    }));
}

app.Run();
