using Microsoft.Extensions.Options;
using Polly;
using System.Net;
using TransmissionManager.Api.Transmission.Options;
using TransmissionManager.Api.Transmission.Services;

namespace TransmissionManager.Api.Transmission.Extensions;

public static class TransmissionClientServiceCollectionExtensions
{
    private const string _transmissionConfigKey = "Transmission";
    private const string _resilienceKey = "Transmission-Retry-Timeout";

    public static IServiceCollection AddTransmissionClient(
        this IServiceCollection services,
        IConfigurationRoot configuration)
    {
        services
            .Configure<TransmissionClientOptions>(configuration.GetSection(_transmissionConfigKey))
            .Configure<TransmissionHeadersServiceOptions>(configuration.GetSection(_transmissionConfigKey))
            .AddSingleton<TransmissionHeadersService>()
            .AddScoped<TransmissionHeadersHandler>()
            .AddHttpClient<TransmissionClient>(ConfigureHttpClient)
            .AddHttpMessageHandler<TransmissionHeadersHandler>()
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
        builder.AddRetry(new()
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
