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
}
