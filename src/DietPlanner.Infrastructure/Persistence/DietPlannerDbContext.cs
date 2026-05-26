using DietPlanner.Application.Abstractions;
using DietPlanner.Domain.Entities;
using DietPlanner.Domain.Enums;
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

    public DbSet<Meal> Meals => Set<Meal>();

    public DbSet<Ingredient> Ingredients => Set<Ingredient>();

    public DbSet<MealIngredient> MealIngredients => Set<MealIngredient>();

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

    public async Task<WeeklyPlan?> FindReadableWeeklyPlanAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("Value cannot be empty.", nameof(userId));
        }

        var plans = await WeeklyPlans
            .Include(plan => plan.Days)
            .ThenInclude(day => day.MealSlots)
            .Where(plan => plan.UserId == userId)
            .OrderByDescending(plan => plan.StartDate)
            .ToListAsync(cancellationToken);

        return plans
            .OrderByDescending(plan => plan.Status == WeeklyPlanStatus.Active)
            .FirstOrDefault();
    }

    public Task<WeeklyPlan?> FindLatestDraftWeeklyPlanAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("Value cannot be empty.", nameof(userId));
        }

        return WeeklyPlans
            .Include(plan => plan.Days)
            .ThenInclude(day => day.MealSlots)
            .Where(plan => plan.UserId == userId && plan.Status == WeeklyPlanStatus.Draft)
            .OrderByDescending(plan => plan.StartDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<Meal?> FindMealByIdAsync(Guid mealId, CancellationToken cancellationToken)
    {
        if (mealId == Guid.Empty)
        {
            throw new ArgumentException("Value cannot be empty.", nameof(mealId));
        }

        return Meals.SingleOrDefaultAsync(meal => meal.Id == mealId, cancellationToken);
    }

    public async Task<IReadOnlyList<Meal>> FindMealsByIdsAsync(IReadOnlyCollection<Guid> mealIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mealIds);

        if (mealIds.Count == 0)
        {
            return [];
        }

        return await Meals
            .Include(meal => meal.Ingredients)
            .Where(meal => mealIds.Contains(meal.Id))
            .ToListAsync(cancellationToken);
    }

    public Task<ShoppingList?> FindShoppingListByWeeklyPlanIdAsync(Guid weeklyPlanId, CancellationToken cancellationToken)
    {
        if (weeklyPlanId == Guid.Empty)
        {
            throw new ArgumentException("Value cannot be empty.", nameof(weeklyPlanId));
        }

        return ShoppingLists
            .Include(list => list.Items)
            .SingleOrDefaultAsync(list => list.WeeklyPlanId == weeklyPlanId, cancellationToken);
    }

    public Task AddShoppingListAsync(ShoppingList shoppingList, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(shoppingList);
        return ShoppingLists.AddAsync(shoppingList, cancellationToken).AsTask();
    }

    public async Task<IReadOnlyList<Meal>> SearchMealsAsync(
        string? name,
        MealType? type,
        string? ingredient,
        CancellationToken cancellationToken)
    {
        IQueryable<Meal> query = Meals.Include(meal => meal.Ingredients);

        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.Where(meal => EF.Functions.Like(meal.Name, $"%{name.Trim()}%"));
        }

        if (type.HasValue)
        {
            query = query.Where(meal => meal.Type == type.Value);
        }

        if (!string.IsNullOrWhiteSpace(ingredient))
        {
            var ingredientFilter = ingredient.Trim();
            var mealIds = from mealIngredient in MealIngredients
                          join ingredientEntity in Ingredients on mealIngredient.IngredientId equals ingredientEntity.Id
                          where EF.Functions.Like(ingredientEntity.Name, $"%{ingredientFilter}%")
                          select mealIngredient.MealId;

            query = query.Where(meal => mealIds.Contains(meal.Id));
        }

        return await query
            .OrderBy(meal => meal.Name)
            .ToListAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DietPlannerDbContext).Assembly);
    }
}
