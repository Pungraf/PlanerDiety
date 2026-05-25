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
        return _dbContext.AddWeeklyPlanAsync(weeklyPlan, cancellationToken);
    }
}
