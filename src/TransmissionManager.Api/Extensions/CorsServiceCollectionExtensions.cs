using Microsoft.Extensions.Options;
using TransmissionManager.Api.Options;
using TransmissionManager.Api.Options.Validation;

namespace Microsoft.Extensions.DependencyInjection;

internal static class CorsServiceCollectionExtensions
{
    private const string _corsSectionName = "Cors";
    private const string _defaultCorsPolicyName = $"{_corsSectionName}:DefaultPolicy";

    public static IServiceCollection AddCorsFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var corsConfig = configuration.GetRequiredSection(_defaultCorsPolicyName).Get<CorsPolicyOptions>()
            ?? throw new InvalidOperationException($"Invalid configuration section: {_defaultCorsPolicyName}");

        var validationResult = new ValidateCorsPolicyOptions().Validate(null, corsConfig);
        if (validationResult.Failed)
        {
            throw new OptionsValidationException(
                nameof(CorsPolicyOptions),
                typeof(CorsPolicyOptions),
                validationResult.Failures);
        }

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder.WithOrigins(corsConfig.Origins)
                    .WithMethods(corsConfig.Methods)
                    .WithHeaders(corsConfig.Headers);

                if (corsConfig.ExposedHeaders.Length > 0)
                    builder.WithExposedHeaders(corsConfig.ExposedHeaders);

                if (corsConfig.AllowCredentials)
                    builder.AllowCredentials();
                else
                    builder.DisallowCredentials();
            });
        });

        return services;
    }
}
