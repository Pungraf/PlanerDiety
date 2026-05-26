using DietPlanner.Application.Abstractions;
using DietPlanner.Application.Shopping;

namespace DietPlanner.Application.Plans.Commands;

public sealed record CopyDayCommand(Guid UserId, DateOnly SourceDate, DateOnly TargetDate);

public sealed class CopyDayHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IShoppingListSyncService _shoppingListSyncService;

    public CopyDayHandler(IApplicationDbContext dbContext, IShoppingListSyncService shoppingListSyncService)
    {
        _dbContext = dbContext;
        _shoppingListSyncService = shoppingListSyncService;
    }

    public async Task<CopyDayResult?> HandleAsync(CopyDayCommand command, CancellationToken cancellationToken)
    {
        var plan = await _dbContext.FindReadableWeeklyPlanAsync(command.UserId, cancellationToken);
        if (plan is null)
        {
            return null;
        }

        if (!plan.CopyDay(command.SourceDate, command.TargetDate))
        {
            return null;
        }

        await _shoppingListSyncService.SyncAsync(plan, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new CopyDayResult(command.SourceDate, command.TargetDate);
    }
}

public sealed record CopyDayResult(DateOnly SourceDate, DateOnly TargetDate);
