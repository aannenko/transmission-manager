using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using System.Net;
using TransmissionManager.Transmission.Options;
using TransmissionManager.Transmission.Options.Validation;
using TransmissionManager.Transmission.Services;

namespace TransmissionManager.Transmission.Extensions;

public static class TransmissionServiceCollectionExtensions
{
    private const string _transmissionConfigKey = "Transmission";
    private const string _resilienceKey = "Transmission-Retry-Timeout";

    public static IServiceCollection AddTransmissionServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .Configure<TransmissionClientOptions>(configuration.GetSection(_transmissionConfigKey))
            .Configure<SessionHeaderProviderOptions>(configuration.GetSection(_transmissionConfigKey))
            .AddSingleton<IValidateOptions<TransmissionClientOptions>, ValidateTransmissionClientOptions>()
            .AddSingleton<IValidateOptions<SessionHeaderProviderOptions>, ValidateSessionHeaderProviderOptions>()
            .AddSingleton<SessionHeaderProvider>()
            .AddScoped<SessionHeaderHandler>()
            .AddHttpClient<TransmissionClient>(ConfigureHttpClient)
            .AddHttpMessageHandler<SessionHeaderHandler>()
            .AddResilienceHandler(_resilienceKey, ConfigureResilience);

        return services;
    }

    private static void ConfigureHttpClient(IServiceProvider services, HttpClient client)
    {
        var options = services.GetRequiredService<IOptionsMonitor<TransmissionClientOptions>>().CurrentValue;
        client.BaseAddress = options.BaseAddressUri;
    }

    private static void ConfigureResilience(ResiliencePipelineBuilder<HttpResponseMessage> builder)
    {
        builder
            .AddRetry(new()
            {
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>().HandleResult(IsRetryRequired)
            })
            .AddTimeout(TimeSpan.FromSeconds(3));

        static bool IsRetryRequired(HttpResponseMessage response) =>
            response is { IsSuccessStatusCode: false, StatusCode: not HttpStatusCode.Conflict };
    }
}
