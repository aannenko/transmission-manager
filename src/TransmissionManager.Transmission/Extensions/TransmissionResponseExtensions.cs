﻿using TransmissionManager.Transmission.Dto;

namespace TransmissionManager.Transmission.Extensions;

public static class TransmissionResponseExtensions
{
    public static bool IsSuccess(this ITransmissionResponse response)
    {
        return response.Result is "success";
    }
}
