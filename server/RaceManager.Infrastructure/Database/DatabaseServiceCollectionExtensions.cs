using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.SqlClient;
using RaceManager.Application.Interfaces;

namespace RaceManager.Infrastructure.Database;

public static class DatabaseServiceCollectionExtensions
{
    public static IServiceCollection AddRaceManagerSqlServer(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("RaceManagerDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return services;
        }

        services.AddDbContext<RaceManagerDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IStartListRepository, SqlStartListRepository>();
        services.AddScoped<IEventJudgeRepository, SqlEventJudgeRepository>();
        services.AddScoped<IChampionshipRepository, SqlChampionshipRepository>();

        if (!CanOpenSqlConnection(connectionString))
        {
            return services;
        }

        services.AddScoped<IUserRepository, SqlUserRepository>();
        services.AddScoped<IEventRepository, SqlEventRepository>();
        services.AddScoped<IResultRepository, SqlResultRepository>();

        return services;
    }

    private static bool CanOpenSqlConnection(string connectionString)
    {
        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString)
            {
                ConnectTimeout = Math.Min(new SqlConnectionStringBuilder(connectionString).ConnectTimeout, 2)
            };
            using var connection = new SqlConnection(builder.ConnectionString);
            connection.Open();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
