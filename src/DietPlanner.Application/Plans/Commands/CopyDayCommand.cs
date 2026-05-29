using DietPlanner.Application.Abstractions;
using DietPlanner.Application.Shopping.Commands;

namespace DietPlanner.Application.Plans.Commands;

public sealed record CopyDayCommand(Guid UserId, DateOnly SourceDate, DateOnly TargetDate, bool DeleteLinkedShoppingLists);

public sealed class CopyDayHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly DeleteShoppingListsForPlanHandler _deleteShoppingListsForPlanHandler;

    public CopyDayHandler(IApplicationDbContext dbContext, DeleteShoppingListsForPlanHandler deleteShoppingListsForPlanHandler)
    {
        _dbContext = dbContext;
        _deleteShoppingListsForPlanHandler = deleteShoppingListsForPlanHandler;
    }

    public async Task<CopyDayResult?> HandleAsync(CopyDayCommand command, CancellationToken cancellationToken)
    {
        var plan = await _dbContext.FindReadableWeeklyPlanAsync(command.UserId, cancellationToken);
        if (plan is null)
        {
            return null;
        }

        var linkedLists = await _dbContext.ListShoppingListsByWeeklyPlanIdAsync(plan.Id, cancellationToken);
        if (linkedLists.Count > 0 && !command.DeleteLinkedShoppingLists)
        {
            return CopyDayResult.LinkedShoppingListsExist();
        }

        if (!plan.CopyDay(command.SourceDate, command.TargetDate))
        {
            return CopyDayResult.NotFound();
        }

        if (linkedLists.Count > 0)
        {
            await _deleteShoppingListsForPlanHandler.DeleteAsync(new DeleteShoppingListsForPlanCommand(plan.Id), cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return CopyDayResult.Success(command.SourceDate, command.TargetDate);
    }
}

public sealed record CopyDayResult(
    bool Succeeded,
    DateOnly? SourceDate,
    DateOnly? TargetDate,
    PlanEditFailureReason? FailureReason)
{
    public static CopyDayResult Success(DateOnly sourceDate, DateOnly targetDate)
        => new(true, sourceDate, targetDate, null);

    public static CopyDayResult NotFound()
        => new(false, null, null, PlanEditFailureReason.NotFound);

    public static CopyDayResult LinkedShoppingListsExist()
        => new(false, null, null, PlanEditFailureReason.LinkedShoppingListsExist);
}
