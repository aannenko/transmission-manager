using TransmissionManager.Database.Dto;
using TransmissionManager.Transmission.Dto;

namespace TransmissionManager.Api.Actions.RefreshTorrentById;

internal static class TransmissionTorrentAddResponseItemExtensions
{
    public static TorrentUpdateDto ToTorrentUpdateDto(this TransmissionTorrentAddResponseItem transmissionTorrent)
    {
        return new(
            hashString: transmissionTorrent.HashString,
            name: transmissionTorrent.Name);
    }
}
