using TransmissionManager.TransmissionClient.Dto;

namespace TransmissionManager.TransmissionClient.Extensions;

public static class TransmissionResponseExtensions
{
    public static bool IsSuccess(this ITransmissionResponse response)
    {
        return response.Result is "success";
    }
}
