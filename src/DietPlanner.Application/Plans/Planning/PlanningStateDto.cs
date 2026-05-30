using DietPlanner.Application.Plans.Queries;

namespace DietPlanner.Application.Plans.Planning;

public sealed record PlanningStateDto(
    CurrentPlanDto CurrentPlan,
    CurrentPlanDto? FuturePlan,
    bool CanGenerateFutureWeek)
{
    public Guid CurrentPlanId => CurrentPlan.Id;

    public Guid? FuturePlanId => FuturePlan?.Id;

    public DateOnly CurrentStartDate => CurrentPlan.StartDate;
}
