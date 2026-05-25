using DietPlanner.Application.Abstractions;
using DietPlanner.Application.Plans.Queries;

namespace DietPlanner.Application.Plans.Commands;

public sealed record ActivateDraftCommand(Guid UserId);

public sealed class ActivateDraftHandler
{
    private readonly IApplicationDbContext _dbContext;

    public ActivateDraftHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CurrentPlanDto?> HandleAsync(ActivateDraftCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var plan = await _dbContext.FindLatestDraftWeeklyPlanAsync(command.UserId, cancellationToken);
        if (plan is null)
        {
            return null;
        }

        plan.Activate();
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CurrentPlanDto.From(plan);
    }
}
