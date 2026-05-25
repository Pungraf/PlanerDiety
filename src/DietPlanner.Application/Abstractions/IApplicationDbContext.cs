using DietPlanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DietPlanner.Application.Abstractions;

public interface IApplicationDbContext
{
    DbSet<WeeklyPlan> WeeklyPlans { get; }

    DbSet<ShoppingList> ShoppingLists { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
