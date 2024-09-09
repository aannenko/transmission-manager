using TransmissionManager.Database.Dto;
using TransmissionManager.TransmissionClient.Dto;

namespace TransmissionManager.Api.Extensions;

public static class TransmissionTorrentAddResponseItemExtensions
{
    public static TorrentUpdateDto ToTorrentUpdateDto(this TransmissionTorrentAddResponseItem transmissionTorrent)
    {
        return new(
            hashString: transmissionTorrent.HashString,
            name: transmissionTorrent.Name);
    }
}
