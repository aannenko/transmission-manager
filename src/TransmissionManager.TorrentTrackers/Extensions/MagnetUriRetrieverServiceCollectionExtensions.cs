using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using System.Diagnostics.CodeAnalysis;
using TransmissionManager.TorrentTrackers.Options;
using TransmissionManager.TorrentTrackers.Services;

namespace TransmissionManager.TorrentTrackers.Extensions;

public static class MagnetUriRetrieverServiceCollectionExtensions
{
    private const string _trackersConfigKey = "Trackers";
    private const string _resilienceKey = "Trackers-Retry-Timeout";

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Trimming tested")]
    public static IServiceCollection AddMagnetUriRetriever(
        this IServiceCollection services,
        IConfigurationRoot configuration)
    {
        services
            .Configure<MagnetUriRetrieverOptions>(configuration.GetSection(_trackersConfigKey))
            .AddHttpClient<MagnetUriRetriever>()
            .AddResilienceHandler(_resilienceKey, ConfigureResilience);

        return services;
    }

    private static void ConfigureResilience(ResiliencePipelineBuilder<HttpResponseMessage> builder)
    {
        builder.AddRetry(new()
        {
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .HandleResult(static response => !response.IsSuccessStatusCode)
        });

        builder.AddTimeout(TimeSpan.FromSeconds(5));
    }
}
