using DietPlanner.Application.Abstractions;
using DietPlanner.Domain.Enums;

namespace DietPlanner.Application.Meals.Queries;

public sealed record SearchMealsQuery(string? Name, MealType? Type, string? Ingredient);

public sealed class SearchMealsHandler
{
    private readonly IApplicationDbContext _dbContext;

    public SearchMealsHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<MealSearchDto>> HandleAsync(SearchMealsQuery query, CancellationToken cancellationToken)
    {
        var meals = await _dbContext.SearchMealsAsync(query.Name, query.Type, query.Ingredient, cancellationToken);
        return meals.Select(MealSearchDto.From).ToArray();
    }
}

public sealed record MealSearchDto(Guid Id, string Name, string Type)
{
    public static MealSearchDto From(DietPlanner.Domain.Entities.Meal meal)
    {
        ArgumentNullException.ThrowIfNull(meal);
        return new MealSearchDto(meal.Id, meal.Name, meal.Type.ToString().ToLowerInvariant());
    }
}
