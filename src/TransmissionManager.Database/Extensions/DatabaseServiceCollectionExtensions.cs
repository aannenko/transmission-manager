using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TransmissionManager.Database.DbContextOptimized;
using TransmissionManager.Database.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class DatabaseServiceCollectionExtensions
{
    private const string _appDbConfigKey = "AppDb";

    public static IServiceCollection AddDatabaseServices(this IServiceCollection services)
    {
        return services
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
