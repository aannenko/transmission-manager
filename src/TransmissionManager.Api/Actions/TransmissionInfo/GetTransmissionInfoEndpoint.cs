using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TransmissionManager.Api.Constants;
using TransmissionManager.Transmission.Options;
using TransmissionManager.Api.Common.Dto.TransmissionInfo;

namespace TransmissionManager.Api.Actions.TransmissionInfo;

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
