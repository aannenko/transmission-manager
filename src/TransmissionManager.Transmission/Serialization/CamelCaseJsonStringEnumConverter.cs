using System.Text.Json;
using System.Text.Json.Serialization;

namespace TransmissionManager.Transmission.Serialization;

internal sealed class CamelCaseJsonStringEnumConverter<TEnum>()
    : JsonStringEnumConverter<TEnum>(JsonNamingPolicy.CamelCase)
    where TEnum : struct, Enum;
