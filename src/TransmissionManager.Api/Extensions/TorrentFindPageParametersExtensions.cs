using TransmissionManager.Api.Dto;
using TransmissionManager.Database.Dto;

namespace TransmissionManager.Api.Extensions;

public static class TorrentFindPageParametersExtensions
{
    public static PageDescriptor ToPageDescriptor(this TorrentFindPageParameters parameters)
    {
        return new(parameters.Take, parameters.AfterId);
    }

    public static TorrentFilter ToTorrentFilter(this TorrentFindPageParameters parameters)
    {
        return new(parameters.WebPageUri, parameters.NameStartsWith, parameters.CronExists);
    }
}
