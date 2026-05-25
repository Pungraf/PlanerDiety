using DietPlanner.Application.Abstractions;
using DietPlanner.Domain.Entities;
using DietPlanner.Domain.Enums;

namespace DietPlanner.Application.Plans.Queries;

public sealed record GetCurrentPlanQuery(Guid UserId);

public sealed class GetCurrentPlanHandler
{
    private readonly IApplicationDbContext _dbContext;

    public GetCurrentPlanHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CurrentPlanDto?> HandleAsync(GetCurrentPlanQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var plan = await _dbContext.FindReadableWeeklyPlanAsync(query.UserId, cancellationToken);
        return plan is null ? null : CurrentPlanDto.From(plan);
    }
}

public sealed record CurrentPlanDto(Guid Id, DateOnly StartDate, string Status, string DinnerMode)
{
    public static CurrentPlanDto From(WeeklyPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        return new CurrentPlanDto(
            plan.Id,
            plan.StartDate,
            ToApiValue(plan.Status),
            ToApiValue(plan.DinnerMode));
    }

    private static string ToApiValue(Enum value)
    {
        var name = value.ToString();
        return string.IsNullOrEmpty(name)
            ? string.Empty
            : char.ToLowerInvariant(name[0]) + name[1..];
    }
}
