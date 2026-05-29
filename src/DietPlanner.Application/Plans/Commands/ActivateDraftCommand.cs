using DietPlanner.Application.Abstractions;
using DietPlanner.Application.Plans.Queries;
using DietPlanner.Application.Shopping.Commands;

namespace DietPlanner.Application.Plans.Commands;

public sealed record ActivateDraftCommand(Guid UserId);

public sealed class ActivateDraftHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly DeleteShoppingListsForPlanHandler _deleteShoppingListsForPlanHandler;

    public ActivateDraftHandler(
        IApplicationDbContext dbContext,
        DeleteShoppingListsForPlanHandler deleteShoppingListsForPlanHandler)
    {
        _dbContext = dbContext;
        _deleteShoppingListsForPlanHandler = deleteShoppingListsForPlanHandler;
    }

    public async Task<CurrentPlanDto?> HandleAsync(ActivateDraftCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var previousReadablePlan = await _dbContext.FindReadableWeeklyPlanAsync(command.UserId, cancellationToken);
        var plan = await _dbContext.FindLatestDraftWeeklyPlanAsync(command.UserId, cancellationToken);
        if (plan is null)
        {
            return null;
        }

        if (previousReadablePlan is not null && previousReadablePlan.Id != plan.Id)
        {
            await _deleteShoppingListsForPlanHandler.DeleteAsync(
                new DeleteShoppingListsForPlanCommand(previousReadablePlan.Id),
                cancellationToken);
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
