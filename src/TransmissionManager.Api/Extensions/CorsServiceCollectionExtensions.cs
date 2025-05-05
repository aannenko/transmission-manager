using Microsoft.Extensions.Options;
using MiniValidation;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace TransmissionManager.Api.Extensions;

internal static class CorsServiceCollectionExtensions
{
    private sealed class CorsPolicyOptions
    {
        [Required]
        [MinLength(1)]
        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Tested after trimming")]
        public required string[] Origins { get; set; }

        [Required]
        [MinLength(1)]
        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Tested after trimming")]
        public required string[] Headers { get; set; }

        [Required]
        [MinLength(1)]
        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Tested after trimming")]
        public required string[] Methods { get; set; }

        [Required]
        public required bool AllowCredentials { get; set; }
    }

    private const string _corsSectionName = "Cors";
    private const string _defaultCorsPolicyName = $"{_corsSectionName}:DefaultPolicy";

    public static IServiceCollection AddCorsFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var corsConfig = configuration.GetRequiredSection(_defaultCorsPolicyName).Get<CorsPolicyOptions>()
            ?? throw new InvalidOperationException($"Invalid configuration section: {_defaultCorsPolicyName}");

        if (!MiniValidator.TryValidate(corsConfig, out var errors))
        {
            throw new OptionsValidationException(
                nameof(CorsPolicyOptions),
                typeof(CorsPolicyOptions),
                errors.Values.SelectMany(static a => a));
        }

        services.AddCors(
            options =>
            {
                options.AddDefaultPolicy(
                    builder =>
                    {
                        builder.WithOrigins(corsConfig.Origins)
                            .WithHeaders(corsConfig.Headers)
                            .WithMethods(corsConfig.Methods);

                        if (corsConfig.AllowCredentials)
                            builder.AllowCredentials();
                        else
                            builder.DisallowCredentials();
                    });
            });

        return services;
    }
}
