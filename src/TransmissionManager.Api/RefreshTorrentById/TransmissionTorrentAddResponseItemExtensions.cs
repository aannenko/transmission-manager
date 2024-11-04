using TransmissionManager.Database.Dto;
using TransmissionManager.Transmission.Dto;

namespace TransmissionManager.Api.RefreshTorrentById;

public static class TransmissionTorrentAddResponseItemExtensions
{
    public static TorrentUpdateDto ToTorrentUpdateDto(this TransmissionTorrentAddResponseItem transmissionTorrent)
    {
        ArgumentNullException.ThrowIfNull(transmissionTorrent);

        return new(
            hashString: transmissionTorrent.HashString,
            name: transmissionTorrent.Name);
    }
}
