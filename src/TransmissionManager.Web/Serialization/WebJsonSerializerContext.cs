using System.Text.Json.Serialization;
using TransmissionManager.Web.Dto;

namespace TransmissionManager.Web.Serialization;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ApiProblemDetails))]
internal sealed partial class WebJsonSerializerContext : JsonSerializerContext;
