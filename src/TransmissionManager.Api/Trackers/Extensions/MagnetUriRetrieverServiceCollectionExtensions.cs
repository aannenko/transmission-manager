using Polly;
using TransmissionManager.Api.Trackers.Options;
using TransmissionManager.Api.Trackers.Services;

namespace TransmissionManager.Api.Trackers.Extensions;

public static class MagnetUriRetrieverServiceCollectionExtensions
{
    private const string _trackersConfigKey = "Trackers";
    private const string _resilienceKey = "Trackers-Retry-Timeout";

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
