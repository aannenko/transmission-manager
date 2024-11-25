using TransmissionManager.Transmission.Dto;

namespace TransmissionManager.Transmission.Extensions;

internal static class TransmissionResponseExtensions
{
    public static bool IsSuccess(this ITransmissionResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        return response.Result is "success";
    }
}
