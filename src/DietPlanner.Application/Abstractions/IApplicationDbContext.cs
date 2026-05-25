using DietPlanner.Domain.Entities;

namespace DietPlanner.Application.Abstractions;

public interface IApplicationDbContext
{
    Task<User?> FindUserByGoogleSubjectAsync(string googleSubject, CancellationToken cancellationToken);

    Task<User?> FindUserByEmailAsync(string email, CancellationToken cancellationToken);

    Task AddUserAsync(User user, CancellationToken cancellationToken);

    Task AddWeeklyPlanAsync(WeeklyPlan weeklyPlan, CancellationToken cancellationToken);

    Task<WeeklyPlan?> FindCurrentWeeklyPlanAsync(Guid userId, CancellationToken cancellationToken);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
