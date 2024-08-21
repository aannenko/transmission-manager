using TransmissionManager.Api.Transmission.Dto;

namespace TransmissionManager.Api.Transmission.Extensions;

public static class TransmissionResponseExtensions
{
    public static bool IsSuccess(this ITransmissionResponse response) =>
        response.Result is "success";
}
