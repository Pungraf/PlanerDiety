using DietPlanner.Application.Abstractions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DietPlanner.Api.Tests.Architecture;

public class ApplicationAbstractionsTests
{
    [Fact]
    public void IApplicationDbContext_ShouldNotExposeDbSetProperties()
    {
        var exposedDbSetProperties = typeof(IApplicationDbContext)
            .GetProperties()
            .Where(property =>
                property.PropertyType.IsGenericType &&
                property.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .ToArray();

        exposedDbSetProperties.Should().BeEmpty();
    }
}
