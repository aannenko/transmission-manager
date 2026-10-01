using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using TransmissionManager.Transmission.Options;
using TransmissionManager.Transmission.Options.Validation;
using TransmissionManager.Transmission.Services;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Registers the Transmission RPC client.</summary>
public static class TransmissionServiceCollectionExtensions
{
    private const string _transmissionConfigKey = "Transmission";

    /// <summary>Adds the Transmission client, session handler and validated options.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddTransmissionServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var transmissionSection = configuration.GetRequiredSection(_transmissionConfigKey);

        _ = services
            .AddSingleton<IValidateOptions<TransmissionClientOptions>, ValidateTransmissionClientOptions>()
            .AddOptions<TransmissionClientOptions>()
            .Bind(transmissionSection)
            .ValidateOnStart();

        _ = services
            .AddSingleton<IValidateOptions<SessionHeaderProviderOptions>, ValidateSessionHeaderProviderOptions>()
            .AddOptions<SessionHeaderProviderOptions>()
            .Bind(transmissionSection)
            .ValidateOnStart();

        _ = services
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
