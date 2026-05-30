using DietPlanner.Application.Plans.Planning;

namespace DietPlanner.Application.Plans.Queries;

public sealed record GetPlanningStateQuery(Guid UserId, DateOnly Today);

public sealed class GetPlanningStateHandler
{
    private readonly PlanningStateService _planningStateService;

    public GetPlanningStateHandler(PlanningStateService planningStateService)
    {
        _planningStateService = planningStateService;
    }

    public Task<PlanningStateDto?> HandleAsync(GetPlanningStateQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return _planningStateService.GetAsync(query.UserId, query.Today, cancellationToken);
    }
}
