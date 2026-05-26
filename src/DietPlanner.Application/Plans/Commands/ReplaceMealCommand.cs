using DietPlanner.Application.Abstractions;
using DietPlanner.Application.Shopping;
using DietPlanner.Domain.Entities;
using DietPlanner.Domain.Enums;

namespace DietPlanner.Application.Plans.Commands;

public sealed record ReplaceMealCommand(Guid UserId, DateOnly Date, MealSlotType SlotType, Guid MealId);

public sealed class ReplaceMealHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IShoppingListSyncService _shoppingListSyncService;

    public ReplaceMealHandler(IApplicationDbContext dbContext, IShoppingListSyncService shoppingListSyncService)
    {
        _dbContext = dbContext;
        _shoppingListSyncService = shoppingListSyncService;
    }

    public async Task<ReplaceMealResult?> HandleAsync(ReplaceMealCommand command, CancellationToken cancellationToken)
    {
        var plan = await _dbContext.FindReadableWeeklyPlanAsync(command.UserId, cancellationToken);
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
            return null;
        }

        slot.ReplaceMeal(meal.Id);
        await _shoppingListSyncService.SyncAsync(plan, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ReplaceMealResult(command.Date, command.SlotType, meal.Id);
    }
}

public sealed record ReplaceMealResult(DateOnly Date, MealSlotType SlotType, Guid MealId);
