using DietPlanner.Application.Plans.Planning;

namespace DietPlanner.Application.Plans.Commands;

public sealed record GenerateFutureWeekCommand(Guid UserId, DateOnly Today);

public sealed class GenerateFutureWeekHandler
{
    private readonly PlanningStateService _planningStateService;

    public GenerateFutureWeekHandler(PlanningStateService planningStateService)
    {
        _planningStateService = planningStateService;
    }

    public Task<PlanningStateDto?> HandleAsync(GenerateFutureWeekCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        return _planningStateService.GenerateFutureAsync(command.UserId, command.Today, cancellationToken);
    }
}
