using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using System.Net;
using TransmissionManager.TorrentSources.JsonPointer;
using TransmissionManager.TorrentSources.WebPage;

namespace Microsoft.Extensions.DependencyInjection;

public static class TorrentSourcesServiceCollectionExtensions
{
    private const string _torrentSourcesConfigKey = "TorrentSources";

    public static IServiceCollection AddTorrentSourcesServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var torrentSourcesSection = configuration.GetRequiredSection(_torrentSourcesConfigKey);

        _ = services
            .AddSingleton<IValidateOptions<TorrentWebPageClientOptions>, ValidateTorrentWebPageClientOptions>()
            .AddOptions<TorrentWebPageClientOptions>()
            .Bind(torrentSourcesSection)
            .ValidateOnStart();

        _ = services
            .AddSingleton<IValidateOptions<TorrentJsonPointerClientOptions>, ValidateTorrentJsonPointerClientOptions>()
            .AddOptions<TorrentJsonPointerClientOptions>()
            .Bind(torrentSourcesSection)
            .ValidateOnStart();

        _ = services
            .AddHttpClient<TorrentWebPageClient>()
            .ConfigurePrimaryHttpMessageHandler(
                static () => new SocketsHttpHandler { AutomaticDecompression = DecompressionMethods.All })
            .AddStandardResilienceHandler(ConfigureResilience);

        _ = services
            .AddHttpClient<TorrentJsonPointerClient>()
            .ConfigurePrimaryHttpMessageHandler(
                static () => new SocketsHttpHandler { AutomaticDecompression = DecompressionMethods.All })
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
