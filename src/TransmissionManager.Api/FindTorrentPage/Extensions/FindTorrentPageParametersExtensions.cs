using TransmissionManager.Api.FindTorrentPage.Request;
using TransmissionManager.Database.Dto;

namespace TransmissionManager.Api.FindTorrentPage.Extensions;

public static class FindTorrentPageParametersExtensions
{
    public static PageDescriptor ToPageDescriptor(this FindTorrentPageParameters parameters)
    {
        return new(parameters.Take, parameters.AfterId);
    }

    public static TorrentFilter ToTorrentFilter(this FindTorrentPageParameters parameters)
    {
        return new(parameters.WebPageUri, parameters.NameStartsWith, parameters.CronExists);
    }
}
