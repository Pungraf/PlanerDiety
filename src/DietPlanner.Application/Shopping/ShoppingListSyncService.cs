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

        existingShoppingList.ReplaceItems(MergeItems(existingShoppingList.Items, generatedShoppingList.Items));
    }

    private static IReadOnlyList<ShoppingListItem> MergeItems(
        IReadOnlyCollection<ShoppingListItem> existingItems,
        IReadOnlyCollection<ShoppingListItem> generatedItems)
    {
        ArgumentNullException.ThrowIfNull(existingItems);
        ArgumentNullException.ThrowIfNull(generatedItems);

        var existingByKey = existingItems.ToDictionary(
            item => new ShoppingListItemKey(item.IngredientId, item.Unit),
            item => item);

        return generatedItems
            .Select(item =>
            {
                var key = new ShoppingListItemKey(item.IngredientId, item.Unit);

                return existingByKey.TryGetValue(key, out var existingItem)
                    ? new ShoppingListItem(existingItem.Id, item.IngredientId, item.Quantity, item.Unit, existingItem.IsChecked)
                    : item;
            })
            .ToArray();
    }

    private sealed record ShoppingListItemKey(Guid IngredientId, string Unit);
}
