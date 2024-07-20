using System.Text.Json.Serialization;
using TransmissionManager.Api.Serialization;

namespace TransmissionManager.Api.Transmission.Models;

public sealed class TorrentGetRequest
{
    public string Method { get; } = "torrent-get";

    public required TorrentGetRequestArguments Arguments { get; init; }

    public int? Tag { get; init; }
}

public sealed class TorrentGetRequestArguments
{
    public required TorrentGetRequestFields[] Fields { get; init; }

    public long[]? Ids { get; init; }
}

[JsonConverter(typeof(CamelCaseJsonStringEnumConverter<TorrentGetRequestFields>))]
public enum TorrentGetRequestFields
{
    Id,
    Name,
    SizeWhenDone,
    PercentDone,
    DownloadDir,
}