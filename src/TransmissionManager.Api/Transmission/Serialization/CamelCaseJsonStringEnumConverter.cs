using System.Text.Json;
using System.Text.Json.Serialization;

namespace TransmissionManager.Api.Transmission.Serialization;

public sealed class CamelCaseJsonStringEnumConverter<TEnum>()
    : JsonStringEnumConverter<TEnum>(JsonNamingPolicy.CamelCase)
    where TEnum : struct, Enum;
