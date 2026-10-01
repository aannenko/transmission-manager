using System.Text.Json.Serialization;

namespace TransmissionManager.Api.Common.Dto.Transmission;

/// <summary>Defines how Transmission handled an add request.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<TransmissionAddResult>))]
public enum TransmissionAddResult
{
    /// <summary>Transmission added the torrent.</summary>
    Added,
    /// <summary>Transmission already held the torrent.</summary>
    Duplicate
}
