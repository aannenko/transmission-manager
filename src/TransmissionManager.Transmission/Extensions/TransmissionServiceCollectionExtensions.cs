using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using TransmissionManager.Transmission.Options;
using TransmissionManager.Transmission.Options.Validation;
using TransmissionManager.Transmission.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class TransmissionServiceCollectionExtensions
{
    private const string _transmissionConfigKey = "Transmission";

    public static IServiceCollection AddTransmissionServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .Configure<TransmissionClientOptions>(configuration.GetRequiredSection(_transmissionConfigKey))
            .Configure<SessionHeaderProviderOptions>(configuration.GetRequiredSection(_transmissionConfigKey))
            .AddSingleton<IValidateOptions<TransmissionClientOptions>, ValidateTransmissionClientOptions>()
            .AddSingleton<IValidateOptions<SessionHeaderProviderOptions>, ValidateSessionHeaderProviderOptions>()
            .AddSingleton<SessionHeaderProvider>()
            .AddScoped<SessionHeaderHandler>()
            .AddHttpClient<TransmissionClient>(ConfigureHttpClient)
            .AddHttpMessageHandler<SessionHeaderHandler>()
            .AddStandardResilienceHandler(ConfigureResilience);

        return services;
    }

    private static void ConfigureHttpClient(IServiceProvider services, HttpClient client)
    {
        var options = services.GetRequiredService<IOptionsMonitor<TransmissionClientOptions>>().CurrentValue;
        client.BaseAddress = options.BaseAddressUri;
    }

    private static void ConfigureResilience(HttpStandardResilienceOptions options)
    {
        options.TotalRequestTimeout = new HttpTimeoutStrategyOptions
        {
            Name = "FiveSeconds-TotalRequestTimeout",
            Timeout = TimeSpan.FromSeconds(5)
        };

        options.AttemptTimeout = new HttpTimeoutStrategyOptions
        {
            Name = "ThreeSeconds-AttemptTimeout",
            Timeout = TimeSpan.FromSeconds(3)
        };
    }
}
