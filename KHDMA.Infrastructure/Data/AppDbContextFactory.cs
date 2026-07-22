using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace KHDMA.Infrastructure.Data
{
    /// <summary>
    /// Lets <c>dotnet ef</c> build the model without starting the API.
    /// </summary>
    /// <remarks>
    /// Without this, EF resolves the context by running <c>Program.cs</c> up to
    /// <c>builder.Build()</c> - which throws on a machine that has no
    /// <c>appsettings.json</c>, because the JWT key is read eagerly. Scaffolding a
    /// migration should not require production secrets.
    ///
    /// The connection string here is only used to pick the SQL Server provider so
    /// the right DDL is generated; no connection is ever opened. Override it with
    /// the <c>KHDMA_MIGRATIONS_CONNECTION</c> environment variable when a command
    /// really does need to reach a database (for example <c>dotnet ef database update</c>).
    /// </remarks>
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        private const string PlaceholderConnection =
            "Server=(localdb)\\MSSQLLocalDB;Database=KHDMA;Trusted_Connection=True;MultipleActiveResultSets=true";

        public AppDbContext CreateDbContext(string[] args)
        {
            var connectionString =
                Environment.GetEnvironmentVariable("KHDMA_MIGRATIONS_CONNECTION")
                ?? PlaceholderConnection;

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            return new AppDbContext(options);
        }
    }
}
