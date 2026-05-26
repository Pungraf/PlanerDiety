using DietPlanner.Domain.Entities;

namespace DietPlanner.Application.Shopping;

public interface IShoppingListSyncService
{
    Task SyncAsync(WeeklyPlan weeklyPlan, CancellationToken cancellationToken);
}
