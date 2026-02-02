using TransmissionManager.Database.Dto;
using TransmissionManager.Transmission.Dto;

namespace TransmissionManager.Api.Actions.Torrents.RefreshById;

internal static class TransmissionTorrentAddResponseItemExtensions
{
    public static TorrentUpdateDto ToTorrentUpdateDto(
        this TransmissionTorrentAddResponseItem transmissionTorrent,
        DateTime refreshDate,
        string? name)
    {
        return new(
            hashString: transmissionTorrent.HashString,
            refreshDate: refreshDate,
            name: name);
    }
}
