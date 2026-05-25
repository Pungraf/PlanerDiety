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

    public DbSet<ShoppingList> ShoppingLists => Set<ShoppingList>();

    public DbSet<DailyPlan> DailyPlans => Set<DailyPlan>();

    public DbSet<DailyMealSlot> DailyMealSlots => Set<DailyMealSlot>();

    public DbSet<ShoppingListItem> ShoppingListItems => Set<ShoppingListItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DietPlannerDbContext).Assembly);
    }
}
