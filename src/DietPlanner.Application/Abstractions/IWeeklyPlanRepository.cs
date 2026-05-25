using DietPlanner.Domain.Entities;

namespace DietPlanner.Application.Abstractions;

public interface IWeeklyPlanRepository
{
    Task AddAsync(WeeklyPlan weeklyPlan, CancellationToken cancellationToken);
}
