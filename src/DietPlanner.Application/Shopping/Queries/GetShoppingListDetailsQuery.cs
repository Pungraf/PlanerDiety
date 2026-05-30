using DietPlanner.Application.Abstractions;
using DietPlanner.Application.Shopping.Commands;

namespace DietPlanner.Application.Shopping.Queries;

public sealed record GetShoppingListDetailsQuery(Guid UserId, Guid ShoppingListId, Guid? PlanId = null);

public sealed class GetShoppingListDetailsHandler
{
    private readonly IApplicationDbContext _dbContext;

    public GetShoppingListDetailsHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ShoppingListDetailsDto?> HandleAsync(
        GetShoppingListDetailsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var plan = query.PlanId.HasValue
            ? await _dbContext.FindWeeklyPlanByIdAsync(query.UserId, query.PlanId.Value, cancellationToken)
            : await _dbContext.FindReadableWeeklyPlanAsync(query.UserId, cancellationToken);
        if (plan is null)
        {
            return null;
        }

        var shoppingList = await _dbContext.FindShoppingListByIdAsync(query.ShoppingListId, cancellationToken);
        if (shoppingList is null || shoppingList.WeeklyPlanId != plan.Id)
        {
            return null;
        }

        var ingredients = await _dbContext.FindIngredientsByIdsAsync(
            shoppingList.Items.Select(item => item.IngredientId).Distinct().ToArray(),
            cancellationToken);

        return ShoppingListDetailsDto.From(
            shoppingList,
            ingredients.ToDictionary(ingredient => ingredient.Id, ingredient => ingredient.Name));
    }
}
