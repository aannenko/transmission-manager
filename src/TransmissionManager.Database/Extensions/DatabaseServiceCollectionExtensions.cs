using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TransmissionManager.Database.DbContextOptimized;
using TransmissionManager.Database.Services;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Registers database services.</summary>
public static class DatabaseServiceCollectionExtensions
{
    private const string _appDbConfigKey = "AppDb";

    /// <summary>Adds the database context, torrent services and compiled model.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services)
    {
        return services
            .AddSingleton<TorrentCountCache>()
            .AddDbContext<AppDbContext>(ConfigureDbContextOptions)
            .AddTransient<TorrentService>();
    }

    private static void ConfigureDbContextOptions(IServiceProvider services, DbContextOptionsBuilder options)
    {
        _ = options
            .UseModel(AppDbContextModel.Instance)
            .UseSqlite(services.GetRequiredService<IConfiguration>().GetConnectionString(_appDbConfigKey));
    }
}
