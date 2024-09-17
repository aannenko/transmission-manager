using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using System.Diagnostics.CodeAnalysis;
using TransmissionManager.TorrentWebPages.Options;
using TransmissionManager.TorrentWebPages.Options.Validation;
using TransmissionManager.TorrentWebPages.Services;

namespace TransmissionManager.TorrentWebPages.Extensions;

public static class TorrentWebPagesServiceCollectionExtensions
{
    private const string _torrentWebPagesConfigKey = "TorrentWebPages";
    private const string _resilienceKey = "TorrentWebPages-Retry-Timeout";

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Tested after trimming")]
    public static IServiceCollection AddTorrentWebPagesServices(
        this IServiceCollection services,
        IConfigurationRoot configuration)
    {
        services
            .Configure<TorrentWebPageClientOptions>(configuration.GetSection(_torrentWebPagesConfigKey))
            .AddSingleton<IValidateOptions<TorrentWebPageClientOptions>, ValidateTorrentWebPageClientOptions>()
            .AddHttpClient<TorrentWebPageClient>()
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
