using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TransmissionManager.Database.Services;

namespace TransmissionManager.Database.Extensions;

public static class DatabaseServiceCollectionExtensions
{
    private const string _appDbConfigKey = "AppDb";

    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddSqlite<AppDbContext>(configuration.GetConnectionString(_appDbConfigKey))
            .AddTransient<TorrentService>();
    }
}
