using DietPlanner.Application.Abstractions;
using DietPlanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DietPlanner.Infrastructure.Persistence;

public class DietPlannerDbContext : DbContext, IApplicationDbContext
{
    public DietPlannerDbContext(DbContextOptions<DietPlannerDbContext> options)
        : base(options)
    {
    }

    public DbSet<WeeklyPlan> WeeklyPlans => Set<WeeklyPlan>();

    public DbSet<User> Users => Set<User>();

    public DbSet<ShoppingList> ShoppingLists => Set<ShoppingList>();

    public DbSet<DailyPlan> DailyPlans => Set<DailyPlan>();

    public DbSet<DailyMealSlot> DailyMealSlots => Set<DailyMealSlot>();

    public DbSet<ShoppingListItem> ShoppingListItems => Set<ShoppingListItem>();

    public Task AddWeeklyPlanAsync(WeeklyPlan weeklyPlan, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(weeklyPlan);
        return WeeklyPlans.AddAsync(weeklyPlan, cancellationToken).AsTask();
    }

    public Task<User?> FindUserByGoogleSubjectAsync(string googleSubject, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(googleSubject);
        return Users.SingleOrDefaultAsync(user => user.GoogleSubject == googleSubject, cancellationToken);
    }

    public Task<User?> FindUserByEmailAsync(string email, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        return Users.SingleOrDefaultAsync(user => user.Email == email, cancellationToken);
    }

    public Task AddUserAsync(User user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        return Users.AddAsync(user, cancellationToken).AsTask();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DietPlannerDbContext).Assembly);
    }
}
