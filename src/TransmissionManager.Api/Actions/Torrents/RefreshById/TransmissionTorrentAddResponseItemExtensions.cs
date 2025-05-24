using TransmissionManager.Database.Dto;

namespace TransmissionManager.Transmission.Dto;

internal static class TransmissionTorrentAddResponseItemExtensions
{
    public static TorrentUpdateDto ToTorrentUpdateDto(
        this TransmissionTorrentAddResponseItem transmissionTorrent,
        DateTime? hashStringDate)
    {
        return new(
            hashString: transmissionTorrent.HashString,
            hashStringDate: hashStringDate,
            name: transmissionTorrent.Name);
    }
}
