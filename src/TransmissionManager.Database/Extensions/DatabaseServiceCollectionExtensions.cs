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
        return services
            .AddDbContext<AppDbContext>(ConfigureDbContextOptions)
            .AddTransient<TorrentService>();
    }

    private static void ConfigureDbContextOptions(IServiceProvider services, DbContextOptionsBuilder options) =>
        options.UseSqlite(services.GetRequiredService<IConfiguration>().GetConnectionString(_appDbConfigKey));
}
