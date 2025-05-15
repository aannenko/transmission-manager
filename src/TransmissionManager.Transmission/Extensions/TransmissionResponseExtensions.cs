namespace TransmissionManager.Transmission.Dto;

internal static class TransmissionResponseExtensions
{
    public static bool IsSuccess(this ITransmissionResponse response) =>
        response.Result is "success";
}
