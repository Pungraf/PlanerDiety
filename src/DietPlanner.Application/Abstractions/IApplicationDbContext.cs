using DietPlanner.Domain.Entities;

namespace DietPlanner.Application.Abstractions;

public interface IApplicationDbContext
{
    Task AddWeeklyPlanAsync(WeeklyPlan weeklyPlan, CancellationToken cancellationToken);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
