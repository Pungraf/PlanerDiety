using DietPlanner.Application.Abstractions;

namespace DietPlanner.Application.Shopping.Queries;

public sealed record GetShoppingListQuery(Guid UserId);

public sealed class GetShoppingListHandler
{
    private readonly IApplicationDbContext _dbContext;

    public GetShoppingListHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ShoppingListDto?> HandleAsync(GetShoppingListQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var plan = await _dbContext.FindReadableWeeklyPlanAsync(query.UserId, cancellationToken);
        if (plan is null)
        {
            return null;
        }

        var shoppingList = await _dbContext.FindShoppingListByWeeklyPlanIdAsync(plan.Id, cancellationToken);
        if (shoppingList is null)
        {
            return null;
        }

        var ingredients = await _dbContext.FindIngredientsByIdsAsync(
            shoppingList.Items.Select(item => item.IngredientId).Distinct().ToArray(),
            cancellationToken);

        return ShoppingListDto.From(shoppingList, ingredients.ToDictionary(ingredient => ingredient.Id, ingredient => ingredient.Name));
    }
}

public sealed record ShoppingListDto(Guid Id, IReadOnlyList<ShoppingListSummaryItemDto> SummaryItems)
{
    public static ShoppingListDto From(Domain.Entities.ShoppingList shoppingList, IReadOnlyDictionary<Guid, string> ingredientNames)
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

        return new ShoppingListDto(shoppingList.Id, items);
    }
}

public sealed record ShoppingListSummaryItemDto(Guid Id, string Name, decimal Quantity, string Unit, bool IsChecked)
{
    public static ShoppingListSummaryItemDto From(Domain.Entities.ShoppingListItem item, string name)
    {
        ArgumentNullException.ThrowIfNull(item);

        return new ShoppingListSummaryItemDto(item.Id, name, item.Quantity, item.Unit, item.IsChecked);
    }
}
