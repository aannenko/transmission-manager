using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Net;
using TransmissionManager.Api.IntegrationTests.Helpers;
using TransmissionManager.Database.Models;

namespace TransmissionManager.Api.IntegrationTests;

[Parallelizable(ParallelScope.Self)]
public sealed class DeleteTorrentByIdTests
{
    private static readonly Torrent[] _torrents = [TestData.Database.CreateInitialTorrents()[0]];

    private TestWebAppliationFactory<Program> _factory;
    private HttpClient _client;

    [OneTimeSetUp]
    public void Setup()
    {
        _factory = new TestWebAppliationFactory<Program>(_torrents, null, null);
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public async ValueTask TearDown()
    {
        _client?.Dispose();
        await _factory.DisposeAsync().ConfigureAwait(false);
    }

    [Test]
    public async Task DeleteTorrentByIdAsync_WhenGivenExistingId_DeletesTorrent()
    {
        const string torrentAddress = $"{TestData.Endpoints.Torrents}/1";

        var response = await _client.DeleteAsync(torrentAddress);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        response = await _client.GetAsync(torrentAddress);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.That(problemDetails, Is.Not.Null);
        Assert.That(problemDetails.Detail, Is.EqualTo("Torrent with id 1 was not found."));
    }

    [Test]
    public async Task UpdateTorrentByIdAsync_WhenGivenNonExistingId_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync($"{TestData.Endpoints.Torrents}/-1");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.That(problemDetails, Is.Not.Null);
        Assert.That(problemDetails.Detail, Is.EqualTo("Torrent with id -1 was not found."));
    }
}
