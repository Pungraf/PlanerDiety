using DietPlanner.Application.Abstractions;
using DietPlanner.Application.Plans.Queries;

namespace DietPlanner.Application.Plans.Commands;

public sealed record ActivateDraftCommand(Guid UserId);

public sealed class ActivateDraftHandler
{
    private readonly IApplicationDbContext _dbContext;

    public ActivateDraftHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CurrentPlanDto?> HandleAsync(ActivateDraftCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var plan = await _dbContext.FindLatestDraftWeeklyPlanAsync(command.UserId, cancellationToken);
        if (plan is null)
        {
            return null;
        }

        plan.Activate();
        await _dbContext.SaveChangesAsync(cancellationToken);

        var mealIds = plan.Days
            .SelectMany(day => day.MealSlots)
            .Where(slot => slot.MealId.HasValue)
            .Select(slot => slot.MealId!.Value)
            .Distinct()
            .ToArray();
        var meals = await _dbContext.FindMealsByIdsAsync(mealIds, cancellationToken);

        return CurrentPlanDto.From(plan, meals);
    }
}
