using TransmissionManager.Database.Dto;

namespace TransmissionManager.Transmission.Dto;

internal static class TransmissionTorrentAddResponseItemExtensions
{
    public static TorrentUpdateDto ToTorrentUpdateDto(
        this TransmissionTorrentAddResponseItem transmissionTorrent,
        DateTime? refreshDate)
    {
        return new(
            hashString: transmissionTorrent.HashString,
            refreshDate: refreshDate,
            name: transmissionTorrent.Name);
    }
}
