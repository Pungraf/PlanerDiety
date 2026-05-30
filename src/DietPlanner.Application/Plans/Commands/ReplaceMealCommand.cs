using DietPlanner.Application.Abstractions;
using DietPlanner.Application.Shopping;
using DietPlanner.Application.Shopping.Commands;
using DietPlanner.Domain.Entities;
using DietPlanner.Domain.Enums;

namespace DietPlanner.Application.Plans.Commands;

public sealed record ReplaceMealCommand(Guid UserId, Guid? PlanId, DateOnly Date, MealSlotType SlotType, Guid MealId, bool DeleteLinkedShoppingLists);

public sealed class ReplaceMealHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly DeleteShoppingListsForPlanHandler _deleteShoppingListsForPlanHandler;

    public ReplaceMealHandler(IApplicationDbContext dbContext, DeleteShoppingListsForPlanHandler deleteShoppingListsForPlanHandler)
    {
        _dbContext = dbContext;
        _deleteShoppingListsForPlanHandler = deleteShoppingListsForPlanHandler;
    }

    public async Task<ReplaceMealResult?> HandleAsync(ReplaceMealCommand command, CancellationToken cancellationToken)
    {
        var plan = command.PlanId.HasValue
            ? await _dbContext.FindWeeklyPlanByIdAsync(command.UserId, command.PlanId.Value, cancellationToken)
            : await _dbContext.FindReadableWeeklyPlanAsync(command.UserId, cancellationToken);
        if (plan is null)
        {
            return null;
        }

        var meal = await _dbContext.FindMealByIdAsync(command.MealId, cancellationToken);
        if (meal is null)
        {
            return null;
        }

        var slot = plan.Days
            .SingleOrDefault(day => day.Date == command.Date)?
            .MealSlots
            .SingleOrDefault(candidate => candidate.SlotType == command.SlotType);

        if (slot is null)
        {
            return ReplaceMealResult.NotFound();
        }

        var linkedLists = await _dbContext.ListShoppingListsByWeeklyPlanIdAsync(plan.Id, cancellationToken);
        if (linkedLists.Count > 0 && !command.DeleteLinkedShoppingLists)
        {
            return ReplaceMealResult.LinkedShoppingListsExist();
        }

        if (linkedLists.Count > 0)
        {
            await _deleteShoppingListsForPlanHandler.DeleteAsync(new DeleteShoppingListsForPlanCommand(plan.Id), cancellationToken);
        }

        slot.ReplaceMeal(meal.Id);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ReplaceMealResult.Success(command.Date, command.SlotType, meal.Id);
    }
}

public enum PlanEditFailureReason
{
    NotFound = 1,
    LinkedShoppingListsExist = 2
}

public sealed record ReplaceMealResult(
    bool Succeeded,
    DateOnly? Date,
    MealSlotType? SlotType,
    Guid? MealId,
    PlanEditFailureReason? FailureReason)
{
    public static ReplaceMealResult Success(DateOnly date, MealSlotType slotType, Guid mealId)
        => new(true, date, slotType, mealId, null);

    public static ReplaceMealResult NotFound()
        => new(false, null, null, null, PlanEditFailureReason.NotFound);

    public static ReplaceMealResult LinkedShoppingListsExist()
        => new(false, null, null, null, PlanEditFailureReason.LinkedShoppingListsExist);
}
