using DietPlanner.Domain.Entities;

namespace DietPlanner.Application.Planning;

public interface IWeeklyPlanGenerator
{
    WeeklyPlan Generate(WeeklyPlanGenerationRequest request);
}
