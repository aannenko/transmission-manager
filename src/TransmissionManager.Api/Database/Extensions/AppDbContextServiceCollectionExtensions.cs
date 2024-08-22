using Microsoft.EntityFrameworkCore;
using TransmissionManager.Api.Database.Services;

namespace TransmissionManager.Api.Database.Extensions;

public static class AppDbContextServiceCollectionExtensions
{
    private const string _appDbConfigKey = "AppDb";

    public static IServiceCollection AddAppDbContext(this IServiceCollection services)
    {
        return services.AddDbContext<AppDbContext>(ConfigureDbContextOptions);
    }

    private static void ConfigureDbContextOptions(IServiceProvider services, DbContextOptionsBuilder options)
    {
        options.UseSqlite(services.GetRequiredService<IConfiguration>().GetConnectionString(_appDbConfigKey));
    }
}
