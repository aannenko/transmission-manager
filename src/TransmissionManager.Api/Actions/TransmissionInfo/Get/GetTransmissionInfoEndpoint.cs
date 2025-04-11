using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TransmissionManager.Api.Constants;
using TransmissionManager.Api.Shared.Dto.TransmissionInfo.Get;
using TransmissionManager.Transmission.Options;

namespace TransmissionManager.Api.Actions.TransmissionInfo.Get;

internal static class GetTransmissionInfoEndpoint
{
    public static IEndpointRouteBuilder MapGetTransmissionInfoEndpoint(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("/", GetTransmissionInfo).WithName(EndpointNames.GetTransmissionInfo);
        return builder;
    }

    private static Ok<GetTransmissionInfoResponse> GetTransmissionInfo(
        [FromServices] IOptionsMonitor<TransmissionClientOptions> transmissionOptions)
    {
        return TypedResults.Ok(new GetTransmissionInfoResponse(new(
            new(transmissionOptions.CurrentValue.BaseAddress),
            transmissionOptions.CurrentValue.RpcEndpointAddressSuffix)));
    }
}
