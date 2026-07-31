using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using TransmissionManager.TorrentSources.Options;
using TransmissionManager.TorrentSources.Options.Validation;
using TransmissionManager.TorrentSources.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class TorrentSourcesServiceCollectionExtensions
{
    private const string _torrentSourcesConfigKey = "TorrentSources";

    public static IServiceCollection AddTorrentSourcesServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _ = services
            .Configure<TorrentWebPageClientOptions>(configuration.GetRequiredSection(_torrentSourcesConfigKey))
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
