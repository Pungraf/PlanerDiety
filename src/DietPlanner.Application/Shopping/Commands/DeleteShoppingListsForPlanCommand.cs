using DietPlanner.Application.Abstractions;

namespace DietPlanner.Application.Shopping.Commands;

public sealed record DeleteShoppingListsForPlanCommand(Guid WeeklyPlanId);

public sealed class DeleteShoppingListsForPlanHandler
{
    private readonly IApplicationDbContext _dbContext;

    public DeleteShoppingListsForPlanHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task DeleteAsync(DeleteShoppingListsForPlanCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var lists = await _dbContext.ListShoppingListsByWeeklyPlanIdAsync(command.WeeklyPlanId, cancellationToken);
        if (lists.Count == 0)
        {
            return;
        }

        _dbContext.RemoveShoppingLists(lists);
    }
}
