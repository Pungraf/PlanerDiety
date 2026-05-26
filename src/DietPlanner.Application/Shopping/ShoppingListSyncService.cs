using DietPlanner.Application.Abstractions;
using DietPlanner.Domain.Entities;

namespace DietPlanner.Application.Shopping;

public sealed class ShoppingListSyncService : IShoppingListSyncService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IShoppingListService _shoppingListService;

    public ShoppingListSyncService(IApplicationDbContext dbContext, IShoppingListService shoppingListService)
    {
        _dbContext = dbContext;
        _shoppingListService = shoppingListService;
    }

    public async Task SyncAsync(WeeklyPlan weeklyPlan, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(weeklyPlan);

        var mealIds = weeklyPlan.Days
            .SelectMany(day => day.MealSlots)
            .Where(slot => slot.MealId.HasValue)
            .Select(slot => slot.MealId!.Value)
            .Distinct()
            .ToArray();

        var meals = await _dbContext.FindMealsByIdsAsync(mealIds, cancellationToken);
        var generatedShoppingList = _shoppingListService.GenerateForPlan(weeklyPlan, meals);
        var existingShoppingList = await _dbContext.FindShoppingListByWeeklyPlanIdAsync(weeklyPlan.Id, cancellationToken);

        if (existingShoppingList is null)
        {
            await _dbContext.AddShoppingListAsync(generatedShoppingList, cancellationToken);
            return;
        }

        existingShoppingList.ReplaceItems(generatedShoppingList.Items);
    }
}
