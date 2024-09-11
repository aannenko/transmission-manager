using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using System.Diagnostics.CodeAnalysis;
using TransmissionManager.TorrentTrackerClient.Options;
using TransmissionManager.TorrentTrackerClient.Options.Validation;
using TransmissionManager.TorrentTrackerClient.Services;

namespace TransmissionManager.TorrentTrackerClient.Extensions;

public static class TorrentTrackerClientServiceCollectionExtensions
{
    private const string _trackersConfigKey = "TorrentWebPages";
    private const string _resilienceKey = "Trackers-Retry-Timeout";

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Tested after trimming")]
    public static IServiceCollection AddTorrentTrackerClientServices(
        this IServiceCollection services,
        IConfigurationRoot configuration)
    {
        services
            .Configure<TorrentWebPageServiceOptions>(configuration.GetSection(_trackersConfigKey))
            .AddSingleton<IValidateOptions<TorrentWebPageServiceOptions>, ValidateTorrentWebPageServiceOptions>()
            .AddHttpClient<TorrentWebPageService>()
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
