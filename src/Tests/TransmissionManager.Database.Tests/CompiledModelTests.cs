using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TransmissionManager.Database.DbContextOptimized;
using TransmissionManager.Database.Services;

namespace TransmissionManager.Database.Tests;

[Parallelizable(ParallelScope.All)]
internal sealed class CompiledModelTests
{
    [Test]
    public void AppDbContext_WhenConstructedWithoutUseModel_UsesCompiledModelViaAssemblyAttribute()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        using var context = new AppDbContext(options);

        Assert.That(context.Model.GetType(), Is.EqualTo(typeof(AppDbContextModel)));
    }
}
