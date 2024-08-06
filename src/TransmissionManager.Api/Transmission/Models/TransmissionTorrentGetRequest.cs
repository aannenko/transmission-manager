using System.Text.Json.Serialization;
using TransmissionManager.Api.Transmission.Serialization;

namespace TransmissionManager.Api.Transmission.Models;

public sealed class TransmissionTorrentGetRequest
{
    public string Method { get; } = "torrent-get";

    public required TransmissionTorrentGetRequestArguments Arguments { get; init; }

    public int? Tag { get; init; }
}

public sealed class TransmissionTorrentGetRequestArguments
{
    public required TransmissionTorrentGetRequestFields[] Fields { get; init; }

    public long[]? Ids { get; init; }
}

[JsonConverter(typeof(CamelCaseJsonStringEnumConverter<TransmissionTorrentGetRequestFields>))]
public enum TransmissionTorrentGetRequestFields
{
    Id,
    Name,
    SizeWhenDone,
    PercentDone,
    DownloadDir,
}