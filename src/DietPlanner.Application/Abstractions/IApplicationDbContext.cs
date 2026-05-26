using DietPlanner.Domain.Enums;
using DietPlanner.Domain.Entities;

namespace DietPlanner.Application.Abstractions;

public interface IApplicationDbContext
{
    Task<User?> FindUserByGoogleSubjectAsync(string googleSubject, CancellationToken cancellationToken);

    Task<User?> FindUserByEmailAsync(string email, CancellationToken cancellationToken);

    Task AddUserAsync(User user, CancellationToken cancellationToken);

    Task AddWeeklyPlanAsync(WeeklyPlan weeklyPlan, CancellationToken cancellationToken);

    Task<WeeklyPlan?> FindReadableWeeklyPlanAsync(Guid userId, CancellationToken cancellationToken);

    Task<WeeklyPlan?> FindLatestDraftWeeklyPlanAsync(Guid userId, CancellationToken cancellationToken);

    Task<Meal?> FindMealByIdAsync(Guid mealId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Meal>> FindMealsByIdsAsync(IReadOnlyCollection<Guid> mealIds, CancellationToken cancellationToken);

    Task<ShoppingList?> FindShoppingListByWeeklyPlanIdAsync(Guid weeklyPlanId, CancellationToken cancellationToken);

    Task AddShoppingListAsync(ShoppingList shoppingList, CancellationToken cancellationToken);

    Task<IReadOnlyList<Meal>> SearchMealsAsync(string? name, MealType? type, string? ingredient, CancellationToken cancellationToken);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
