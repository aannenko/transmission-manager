using System.Globalization;
using TransmissionManager.Database.Dto;

namespace TransmissionManager.Database.Tests;

[Parallelizable(ParallelScope.All)]
internal sealed class TorrentPageDescriptorTests
{
    [Test]
    public void Constructor_WhenValidArgumentsProvided_DoesNotThrow()
    {
        TorrentPageDescriptor<string> page = default;

        Assert.That(
            () => page = new TorrentPageDescriptor<string>(
                OrderBy: TorrentOrder.IdDesc,
                AnchorId: 2,
                AnchorValue: null,
                IsForwardPagination: false,
                Take: 5),
            Throws.Nothing);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(page.OrderBy, Is.EqualTo(TorrentOrder.IdDesc));
            Assert.That(page.AnchorId, Is.EqualTo(2));
            Assert.That(page.AnchorValue, Is.Null);
            Assert.That(page.IsForwardPagination, Is.False);
            Assert.That(page.Take, Is.EqualTo(5));
        }
    }

    [Test]
    public void Constructor_WhenTorrentOrderIsInvalid_ThrowsArgumentOutOfRangeException()
    {
        var orderBy = (TorrentOrder)int.MaxValue;

        Assert.That(
            () => new TorrentPageDescriptor<int>(OrderBy: orderBy),
            Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void Constructor_WhenOrderByAndAnchorValueAreIncompatible_ThrowsArgumentException()
    {
        using (Assert.EnterMultipleScope())
        {
            const string error1 = $"Incompatible arguments OrderBy 'Id' " +
                $"and AnchorValue System.String 'abc' were provided. (Parameter 'AnchorValue')";

            Assert.That(
                () => new TorrentPageDescriptor<string>(OrderBy: TorrentOrder.Id, AnchorValue: "abc"),
                Throws.InstanceOf<ArgumentException>().With.Message.EqualTo(error1));

            const string error2 = $"Incompatible arguments OrderBy 'Name' " +
                $"and AnchorValue System.Int32 '0' were provided. (Parameter 'AnchorValue')";

            Assert.That(
                () => new TorrentPageDescriptor<int>(OrderBy: TorrentOrder.Name, AnchorValue: 0),
                Throws.InstanceOf<ArgumentException>().With.Message.EqualTo(error2));

            var utcNow = DateTime.UtcNow;
            var error3 = $"Incompatible arguments OrderBy 'Name' and AnchorValue System.DateTime " +
                $"'{utcNow.ToString(CultureInfo.InvariantCulture)}' were provided. (Parameter 'AnchorValue')";

            Assert.That(
                () => new TorrentPageDescriptor<DateTime>(OrderBy: TorrentOrder.Name, AnchorValue: utcNow),
                Throws.InstanceOf<ArgumentException>().With.Message.EqualTo(error3));
        }
    }

    [Test]
    public void Constructor_WhenTakeIsZeroOrNegative_ThrowsArgumentOutOfRangeException()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                () => new TorrentPageDescriptor<string>(Take: 0),
                Throws.InstanceOf<ArgumentOutOfRangeException>());

            Assert.That(
                () => new TorrentPageDescriptor<string>(Take: int.MinValue),
                Throws.InstanceOf<ArgumentOutOfRangeException>());
        }
    }
}
