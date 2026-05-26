using FluentAssertions;

namespace DietPlanner.Api.Tests.Architecture;

public class ApiCompositionTests
{
    [Fact]
    public void ApiProject_ShouldIncludeDeploymentAndLocalDevelopmentFiles()
    {
        var repositoryRoot = GetRepositoryRoot();

        File.Exists(Path.Combine(repositoryRoot, "src", "DietPlanner.Api", "appsettings.Development.json"))
            .Should().BeTrue();
        File.Exists(Path.Combine(repositoryRoot, "README.md"))
            .Should().BeTrue();
        File.Exists(Path.Combine(repositoryRoot, "docker-compose.yml"))
            .Should().BeTrue();
    }

    [Fact]
    public void Program_ShouldDelegateServiceRegistrationToCompositionExtensions()
    {
        var repositoryRoot = GetRepositoryRoot();
        var programFile = Path.Combine(repositoryRoot, "src", "DietPlanner.Api", "Program.cs");
        var programContents = File.ReadAllText(programFile);

        programContents.Should().Contain("builder.Services.AddApplicationServices(builder.Configuration);");
        programContents.Should().NotContain("builder.Services.AddScoped<GoogleLoginHandler>();");
    }

    private static string GetRepositoryRoot()
        => Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            ".."));
}
