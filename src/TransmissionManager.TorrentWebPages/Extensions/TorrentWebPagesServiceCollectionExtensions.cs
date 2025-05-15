using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using TransmissionManager.TorrentWebPages.Options;
using TransmissionManager.TorrentWebPages.Options.Validation;
using TransmissionManager.TorrentWebPages.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class TorrentWebPagesServiceCollectionExtensions
{
    private const string _torrentWebPagesConfigKey = "TorrentWebPages";

    public static IServiceCollection AddTorrentWebPagesServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .Configure<TorrentWebPageClientOptions>(configuration.GetRequiredSection(_torrentWebPagesConfigKey))
            .AddSingleton<IValidateOptions<TorrentWebPageClientOptions>, ValidateTorrentWebPageClientOptions>()
            .AddHttpClient<TorrentWebPageClient>()
            .AddStandardResilienceHandler(ConfigureResilience);

        return services;
    }

    private static void ConfigureResilience(HttpStandardResilienceOptions options)
    {
        options.TotalRequestTimeout = new HttpTimeoutStrategyOptions
        {
            Name = "FifteenSeconds-TotalRequestTimeout",
            Timeout = TimeSpan.FromSeconds(15)
        };

        options.AttemptTimeout = new HttpTimeoutStrategyOptions
        {
            Name = "SevenSeconds-AttemptTimeout",
            Timeout = TimeSpan.FromSeconds(7)
        };
    }
}
