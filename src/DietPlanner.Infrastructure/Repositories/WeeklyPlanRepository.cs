using DietPlanner.Application.Abstractions;
using DietPlanner.Domain.Entities;

namespace DietPlanner.Infrastructure.Repositories;

public class WeeklyPlanRepository : IWeeklyPlanRepository
{
    private readonly IApplicationDbContext _dbContext;

    public WeeklyPlanRepository(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(WeeklyPlan weeklyPlan, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(weeklyPlan);
        return _dbContext.WeeklyPlans.AddAsync(weeklyPlan, cancellationToken).AsTask();
    }
}
