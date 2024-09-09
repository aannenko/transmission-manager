using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TransmissionManager.Database.Services;

namespace TransmissionManager.Database.Extensions;

public static class DatabaseServiceCollectionExtensions
{
    private const string _appDbConfigKey = "AppDb";

    public static IServiceCollection AddDatabaseServices(this IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>(ConfigureDbContextOptions);
        services.AddTransient<TorrentService>();
        return services;
    }

    private static void ConfigureDbContextOptions(IServiceProvider services, DbContextOptionsBuilder options)
    {
        options.UseSqlite(services.GetRequiredService<IConfiguration>().GetConnectionString(_appDbConfigKey));
    }
}
