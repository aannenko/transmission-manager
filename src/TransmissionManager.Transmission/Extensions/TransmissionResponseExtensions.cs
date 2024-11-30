using TransmissionManager.Transmission.Dto;

namespace TransmissionManager.Transmission.Extensions;

internal static class TransmissionResponseExtensions
{
    public static bool IsSuccess(this ITransmissionResponse response) =>
        response.Result is "success";
}
