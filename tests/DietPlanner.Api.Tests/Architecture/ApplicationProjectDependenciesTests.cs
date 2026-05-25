using FluentAssertions;

namespace DietPlanner.Api.Tests.Architecture;

public class ApplicationProjectDependenciesTests
{
    [Fact]
    public void DietPlannerApplicationProject_ShouldNotReferenceEntityFrameworkCore()
    {
        var projectFile = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "DietPlanner.Application",
            "DietPlanner.Application.csproj"));

        var projectContents = File.ReadAllText(projectFile);

        projectContents.Should().NotContain("Microsoft.EntityFrameworkCore");
    }
}
