using TransmissionManager.Api.Database.Dto;
using TransmissionManager.Api.Endpoints.Dto;

namespace TransmissionManager.Api.Endpoints.Extensions;

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
