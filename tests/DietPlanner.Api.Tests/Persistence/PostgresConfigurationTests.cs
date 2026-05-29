using DietPlanner.Api.Extensions;
using DietPlanner.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DietPlanner.Api.Tests.Persistence;

public class PostgresConfigurationTests
{
    [Fact]
    public void AddApplicationServices_ShouldUsePostgresProvider_WhenConnectionStringTargetsPostgres()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DietPlanner"] = "Host=localhost;Port=5432;Database=dietplanner;Username=postgres;Password=postgres",
                ["Jwt:Key"] = "test-signing-key-with-minimum-length-123456"
            })
            .Build();

        var services = new ServiceCollection();

        services.AddApplicationServices(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DietPlannerDbContext>();

        dbContext.Database.ProviderName.Should().Be("Npgsql.EntityFrameworkCore.PostgreSQL");
    }

    [Fact]
    public void PostgresSchemaScript_ShouldNotContainSqlServerStyleFilteredIndexSyntax()
    {
        var options = new DbContextOptionsBuilder<DietPlannerDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=dietplanner;Username=postgres;Password=postgres")
            .Options;

        using var dbContext = new DietPlannerDbContext(options);

        var script = dbContext.Database.GenerateCreateScript();

        script.Should().NotContain("[Email]");
        script.Should().NotContain("[GoogleSubject]");
    }

    [Fact]
    public void DatabaseBootstrapper_ShouldPreserveMigrations_ForConfiguredPostgresSchemas()
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            ".."));
        var bootstrapperFile = Path.Combine(
            repositoryRoot,
            "src",
            "DietPlanner.Infrastructure",
            "Persistence",
            "SqliteSchemaBootstrapper.cs");

        var contents = File.ReadAllText(bootstrapperFile);

        contents.Should().Contain("await dbContext.Database.MigrateAsync(cancellationToken);");
        contents.Should().Contain("if (hasMigrationHistory)");
    }

    [Fact]
    public void DatabaseBootstrapper_ShouldHandleLegacyPostgresSchemaWithoutMigrationHistory()
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            ".."));
        var bootstrapperFile = Path.Combine(
            repositoryRoot,
            "src",
            "DietPlanner.Infrastructure",
            "Persistence",
            "SqliteSchemaBootstrapper.cs");

        var contents = File.ReadAllText(bootstrapperFile);

        contents.Should().Contain("await dbContext.Database.EnsureCreatedAsync(cancellationToken);");
        contents.Should().Contain("await StampAppliedMigrationsAsync(");
        contents.Should().Contain("UpgradeLegacyPostgresSchemaAsync");
    }
}
