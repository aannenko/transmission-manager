using System.Text.Json.Serialization;
using System.Text.Json;

namespace TransmissionManager.Api.Serialization;

public sealed class CamelCaseJsonStringEnumConverter<TEnum>()
    : JsonStringEnumConverter<TEnum>(JsonNamingPolicy.CamelCase)
    where TEnum : struct, Enum;
