using DietPlanner.Application.Abstractions;

namespace DietPlanner.Application.Shopping.Commands;

public sealed record DeleteShoppingListCommand(Guid UserId, Guid ShoppingListId);

public sealed class DeleteShoppingListHandler
{
    private readonly IApplicationDbContext _dbContext;

    public DeleteShoppingListHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> HandleAsync(DeleteShoppingListCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var plan = await _dbContext.FindReadableWeeklyPlanAsync(command.UserId, cancellationToken);
        if (plan is null)
        {
            return false;
        }

        var shoppingList = await _dbContext.FindShoppingListByIdAsync(command.ShoppingListId, cancellationToken);
        if (shoppingList is null || shoppingList.WeeklyPlanId != plan.Id)
        {
            return false;
        }

        _dbContext.RemoveShoppingLists([shoppingList]);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
