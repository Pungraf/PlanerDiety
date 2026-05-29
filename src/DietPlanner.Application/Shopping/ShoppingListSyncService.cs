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
        await Task.CompletedTask;
    }
}
