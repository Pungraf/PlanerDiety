using DietPlanner.Application.Abstractions;
using DietPlanner.Application.Plans.Queries;
using DietPlanner.Domain.Enums;

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

        var plan = await _dbContext.FindCurrentWeeklyPlanAsync(command.UserId, cancellationToken);
        if (plan is null)
        {
            return null;
        }

        if (plan.Status != WeeklyPlanStatus.Draft)
        {
            throw new InvalidOperationException("Only draft plans can be activated.");
        }

        plan.Activate();
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CurrentPlanDto.From(plan);
    }
}
