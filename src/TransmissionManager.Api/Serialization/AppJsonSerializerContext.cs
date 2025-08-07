using System.Text.Json.Serialization;

namespace TransmissionManager.Api.Serialization;

[JsonSerializable(typeof(HttpValidationProblemDetails))]
internal sealed partial class ApiJsonSerializerContext : JsonSerializerContext;
