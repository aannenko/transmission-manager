using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using TransmissionManager.TransmissionClient.Options;
using TransmissionManager.TransmissionClient.Services;

namespace TransmissionManager.TransmissionClient.Extensions;

public static class TransmissionClientServiceCollectionExtensions
{
    private const string _transmissionConfigKey = "Transmission";
    private const string _resilienceKey = "Transmission-Retry-Timeout";

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Tested after trimming")]
    public static IServiceCollection AddTransmissionService(
        this IServiceCollection services,
        IConfigurationRoot configuration)
    {
        services
            .Configure<TransmissionClientOptions>(configuration.GetSection(_transmissionConfigKey))
            .Configure<TransmissionHeadersProviderOptions>(configuration.GetSection(_transmissionConfigKey))
            .AddSingleton<SessionHeaderProvider>()
            .AddScoped<SessionHeaderHandler>()
            .AddHttpClient<TransmissionService>(ConfigureHttpClient)
            .AddHttpMessageHandler<SessionHeaderHandler>()
            .AddResilienceHandler(_resilienceKey, ConfigureResilience);

        return services;
    }

    private static void ConfigureHttpClient(IServiceProvider services, HttpClient client)
    {
        var options = services.GetRequiredService<IOptionsMonitor<TransmissionClientOptions>>().CurrentValue;
        client.BaseAddress = new(options.BaseAddress);
    }

    private static void ConfigureResilience(ResiliencePipelineBuilder<HttpResponseMessage> builder)
    {
        builder.AddRetry(
            new()
            {
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(static response => response is
                    {
                        IsSuccessStatusCode: false,
                        StatusCode: not HttpStatusCode.Conflict,
                    })
            });

        builder.AddTimeout(TimeSpan.FromSeconds(3));
    }
}
