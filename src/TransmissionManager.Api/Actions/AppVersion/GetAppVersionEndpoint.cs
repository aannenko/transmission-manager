using Microsoft.AspNetCore.Http.HttpResults;
using TransmissionManager.Api.Constants;

namespace TransmissionManager.Api.Actions.AppVersion;

internal static class GetAppVersionEndpoint
{
    private static readonly Version _assemblyVersion = typeof(Program).Assembly.GetName().Version!;

    public static IEndpointRouteBuilder MapGetAppVersionEndpoint(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("/", GetAppVersion).WithName(EndpointNames.GetAppVersion);
        return builder;
    }

    private static Ok<Version> GetAppVersion() =>
        TypedResults.Ok(_assemblyVersion);
}
