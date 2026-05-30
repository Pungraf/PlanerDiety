using DietPlanner.Application.Abstractions;
using DietPlanner.Application.Shopping.Queries;

namespace DietPlanner.Application.Shopping.Commands;

public sealed record ToggleShoppingListItemCommand(Guid UserId, Guid ShoppingListId, Guid ItemId);

public sealed class ToggleShoppingListItemHandler
{
    private readonly IApplicationDbContext _dbContext;

    public ToggleShoppingListItemHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ShoppingListDetailsDto?> HandleAsync(ToggleShoppingListItemCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var plan = await _dbContext.FindReadableWeeklyPlanAsync(command.UserId, cancellationToken);
        if (plan is null)
        {
            return null;
        }

        var shoppingList = await _dbContext.FindShoppingListByIdAsync(command.ShoppingListId, cancellationToken);
        if (shoppingList is null || shoppingList.WeeklyPlanId != plan.Id)
        {
            return null;
        }

        var item = shoppingList.Items.SingleOrDefault(candidate => candidate.Id == command.ItemId);
        if (item is null)
        {
            return null;
        }

        item.ToggleChecked();
        await _dbContext.SaveChangesAsync(cancellationToken);

        var ingredients = await _dbContext.FindIngredientsByIdsAsync(
            shoppingList.Items.Select(candidate => candidate.IngredientId).Distinct().ToArray(),
            cancellationToken);

        return ShoppingListDetailsDto.From(shoppingList, ingredients.ToDictionary(ingredient => ingredient.Id, ingredient => ingredient.Name));
    }
}
