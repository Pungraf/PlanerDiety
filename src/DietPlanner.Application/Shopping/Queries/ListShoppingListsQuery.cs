using DietPlanner.Application.Abstractions;

namespace DietPlanner.Application.Shopping.Queries;

public sealed record ListShoppingListsQuery(Guid UserId);

public sealed class ListShoppingListsHandler
{
    private readonly IApplicationDbContext _dbContext;

    public ListShoppingListsHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ShoppingListSummaryDto>> HandleAsync(
        ListShoppingListsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var plan = await _dbContext.FindReadableWeeklyPlanAsync(query.UserId, cancellationToken);
        if (plan is null)
        {
            return [];
        }

        var lists = await _dbContext.ListShoppingListsByWeeklyPlanIdAsync(plan.Id, cancellationToken);
        return lists.Select(ShoppingListSummaryDto.From).ToArray();
    }
}

public sealed record ShoppingListSummaryDto(Guid Id, string Name, DateTimeOffset CreatedAt, int ItemCount)
{
    public static ShoppingListSummaryDto From(Domain.Entities.ShoppingList shoppingList)
    {
        ArgumentNullException.ThrowIfNull(shoppingList);
        return new ShoppingListSummaryDto(
            shoppingList.Id,
            shoppingList.Name,
            shoppingList.CreatedAt,
            shoppingList.Items.Count);
    }
}
