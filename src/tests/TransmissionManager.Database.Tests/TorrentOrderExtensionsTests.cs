using TransmissionManager.Database.Dto;

namespace TransmissionManager.Database.Tests;

[Parallelizable(ParallelScope.All)]
internal sealed class TorrentOrderExtensionsTests
{
    [Test]
    public void IsDescending_WhenSpecificOrderIsUsed_ReturnsExpectedBoolean()
    {
        using (Assert.EnterMultipleScope())
        {
            foreach (var order in Enum.GetValues<TorrentOrder>())
            {
                Assert.That(
                    order.IsDescending(),
                    Is.EqualTo(order.ToString().EndsWith("Desc", StringComparison.Ordinal)),
                    actualExpression: $"{order}.IsDescending()");
            }
        }
    }

    [Test]
    public void Reverse_WhenSpecificOrderIsUsed_ReturnsExpectedReversedOrder()
    {
        using (Assert.EnterMultipleScope())
        {
            foreach (var order in Enum.GetValues<TorrentOrder>())
            {
                var orderString = order.ToString();
                var reverseOrderString = orderString.EndsWith("Desc", StringComparison.Ordinal)
                    ? orderString[..^4]
                    : $"{order}Desc";

                bool isDefined;
                Assert.That(
                    isDefined = Enum.TryParse<TorrentOrder>(reverseOrderString, out var reverseOrder),
                    Is.True,
                    actualExpression: $"Enum.TryParse<TorrentOrder>({reverseOrderString})");

                if (isDefined)
                    Assert.That(order.Reverse(), Is.EqualTo(reverseOrder), actualExpression: $"{order}.Reverse()");
            }
        }
    }

    [Test]
    public void IsCompatibleWith_WhenSpecificTorrentOrderAndAnchorValueIsUsed_ReturnsExpectedBoolean()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(TorrentOrder.Id.IsCompatibleWith<long?>(null), Is.True);
            Assert.That(TorrentOrder.Id.IsCompatibleWith(20L), Is.False);
            Assert.That(TorrentOrder.Id.IsCompatibleWith("abc"), Is.False);

            Assert.That(TorrentOrder.IdDesc.IsCompatibleWith<long?>(null), Is.True);
            Assert.That(TorrentOrder.IdDesc.IsCompatibleWith(20L), Is.False);
            Assert.That(TorrentOrder.IdDesc.IsCompatibleWith("abc"), Is.False);

            Assert.That(TorrentOrder.RefreshDate.IsCompatibleWith(DateTime.Now), Is.True);
            Assert.That(TorrentOrder.RefreshDate.IsCompatibleWith<DateTime?>(null), Is.True);
            Assert.That(TorrentOrder.RefreshDate.IsCompatibleWith("abc"), Is.False);

            Assert.That(TorrentOrder.RefreshDateDesc.IsCompatibleWith(DateTime.Now), Is.True);
            Assert.That(TorrentOrder.RefreshDateDesc.IsCompatibleWith<DateTime?>(null), Is.True);
            Assert.That(TorrentOrder.RefreshDateDesc.IsCompatibleWith("abc"), Is.False);

            foreach (var order in Enum.GetValues<TorrentOrder>()
                .Except([TorrentOrder.Id, TorrentOrder.IdDesc, TorrentOrder.RefreshDate, TorrentOrder.RefreshDateDesc]))
            {
                Assert.That(
                    order.IsCompatibleWith("abc"),
                    Is.True,
                    actualExpression: $"{order}.IsCompatibleWith(\"abc\")");

                Assert.That(
                    order.IsCompatibleWith<string>(null),
                    Is.True,
                    actualExpression: $"{order}.IsCompatibleWith<string>(null)");

                Assert.That(
                    order.IsCompatibleWith(20L),
                    Is.False,
                    actualExpression: $"{order}.IsCompatibleWith(20L)");
            }
        }
    }
}
