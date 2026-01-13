using TransmissionManager.Api.Common.Constants;

namespace TransmissionManager.Api.Actions.AppVersion;

internal static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapAppVersionEndpoints(this IEndpointRouteBuilder builder)
    {
        _ = builder
            .MapGroup(EndpointAddresses.AppVersion)
            .MapGetAppVersionEndpoint();

        return builder;
    }
}
