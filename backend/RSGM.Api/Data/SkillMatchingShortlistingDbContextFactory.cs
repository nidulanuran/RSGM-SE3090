using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using RSGM.Api.Models;

namespace RSGM.Api.Data;

/// <summary>
/// Design-time factory used by EF Core migrations for the
/// isolated SkillMatchingShortlistingDbContext.
///
/// The main application's database connection string is reused,
/// but the Agent context has its own migrations history table.
/// </summary>
public sealed class SkillMatchingShortlistingDbContextFactory
    : IDesignTimeDbContextFactory<SkillMatchingShortlistingDbContext>
{
    public SkillMatchingShortlistingDbContext CreateDbContext(
        string[] args)
    {
        var configuration =
            new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(
                    "appsettings.json",
                    optional: true,
                    reloadOnChange: false)
                .AddUserSecrets<
                    SkillMatchingShortlistingDbContextFactory>(
                    optional: true)
                .AddEnvironmentVariables()
                .Build();

        var connectionString =
            configuration.GetConnectionString(
                "DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "DefaultConnection is not configured.");
        }

        var options =
            new DbContextOptionsBuilder<
                SkillMatchingShortlistingDbContext>();

        options.UseNpgsql(
            connectionString,
            npgsql =>
            {
                npgsql.MigrationsHistoryTable(
                    "__SkillMatchingAgentMigrationsHistory");
            });

        return new SkillMatchingShortlistingDbContext(
            options.Options);
    }
}