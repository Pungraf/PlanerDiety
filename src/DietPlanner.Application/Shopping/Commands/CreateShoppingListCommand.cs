using DietPlanner.Application.Abstractions;
using DietPlanner.Application.Shopping.Queries;
using DietPlanner.Domain.Entities;
using DietPlanner.Domain.Enums;

namespace DietPlanner.Application.Shopping.Commands;

public sealed record SelectedShoppingIngredient(DateOnly Date, MealSlotType SlotType, Guid IngredientId);

public sealed record CreateShoppingListCommand(Guid UserId, Guid? PlanId, string Name, IReadOnlyList<SelectedShoppingIngredient> Ingredients);

public sealed class CreateShoppingListHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IShoppingListService _shoppingListService;

    public CreateShoppingListHandler(IApplicationDbContext dbContext, IShoppingListService shoppingListService)
    {
        _dbContext = dbContext;
        _shoppingListService = shoppingListService;
    }

    public async Task<ShoppingListDetailsDto?> HandleAsync(CreateShoppingListCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var plan = command.PlanId.HasValue
            ? await _dbContext.FindWeeklyPlanByIdAsync(command.UserId, command.PlanId.Value, cancellationToken)
            : await _dbContext.FindReadableWeeklyPlanAsync(command.UserId, cancellationToken);
        if (plan is null)
        {
            return null;
        }

        var mealIds = plan.Days
            .SelectMany(day => day.MealSlots)
            .Where(slot => slot.MealId.HasValue)
            .Select(slot => slot.MealId!.Value)
            .Distinct()
            .ToArray();

        var meals = await _dbContext.FindMealsByIdsAsync(mealIds, cancellationToken);
        var shoppingList = _shoppingListService.GenerateForSelections(plan, meals, command.Name, command.Ingredients);

        await _dbContext.AddShoppingListAsync(shoppingList, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var ingredients = await _dbContext.FindIngredientsByIdsAsync(
            shoppingList.Items.Select(item => item.IngredientId).Distinct().ToArray(),
            cancellationToken);

        return ShoppingListDetailsDto.From(
            shoppingList,
            ingredients.ToDictionary(ingredient => ingredient.Id, ingredient => ingredient.Name));
    }
}

public sealed record ShoppingListDetailsDto(Guid Id, string Name, IReadOnlyList<ShoppingListSummaryItemDto> Items)
{
    public static ShoppingListDetailsDto From(
        ShoppingList shoppingList,
        IReadOnlyDictionary<Guid, string> ingredientNames)
    {
        ArgumentNullException.ThrowIfNull(shoppingList);
        ArgumentNullException.ThrowIfNull(ingredientNames);

        var items = shoppingList.Items
            .Select(item => ShoppingListSummaryItemDto.From(
                item,
                ingredientNames.TryGetValue(item.IngredientId, out var name) ? name : string.Empty))
            .OrderBy(item => item.IsChecked)
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Id)
            .ToArray();

        return new ShoppingListDetailsDto(shoppingList.Id, shoppingList.Name, items);
    }
}
