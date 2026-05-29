using DietPlanner.Application.Meals.Queries;
using DietPlanner.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DietPlanner.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/meals")]
public sealed class MealsController : ControllerBase
{
    private readonly SearchMealsHandler _searchMealsHandler;
    private readonly GetMealDetailsHandler _getMealDetailsHandler;

    public MealsController(SearchMealsHandler searchMealsHandler, GetMealDetailsHandler getMealDetailsHandler)
    {
        _searchMealsHandler = searchMealsHandler;
        _getMealDetailsHandler = getMealDetailsHandler;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MealSearchResponse>>> Search(
        [FromQuery] string? name,
        [FromQuery] string? type,
        [FromQuery] string? ingredient,
        CancellationToken cancellationToken)
    {
        if (!TryParseMealType(type, out var parsedType))
        {
            return BadRequest();
        }

        var meals = await _searchMealsHandler.HandleAsync(
            new SearchMealsQuery(name, parsedType, ingredient),
            cancellationToken);

        return Ok(meals.Select(MealSearchResponse.From).ToArray());
    }

    [HttpGet("{mealId:guid}")]
    public async Task<ActionResult<MealDetailsResponse>> GetDetails(Guid mealId, CancellationToken cancellationToken)
    {
        if (mealId == Guid.Empty)
        {
            return BadRequest();
        }

        var meal = await _getMealDetailsHandler.HandleAsync(new GetMealDetailsQuery(mealId), cancellationToken);
        return meal is null ? NotFound() : Ok(MealDetailsResponse.From(meal));
    }

    private static bool TryParseMealType(string? type, out MealType? parsedType)
    {
        parsedType = null;

        if (string.IsNullOrWhiteSpace(type))
        {
            return true;
        }

        if (!Enum.TryParse<MealType>(type, true, out var mealType))
        {
            return false;
        }

        if (!Enum.IsDefined(mealType))
        {
            return false;
        }

        parsedType = mealType;
        return true;
    }
}

public sealed record MealSearchResponse(Guid Id, string Name, string Type, int Kcal, int Protein)
{
    public static MealSearchResponse From(MealSearchDto meal)
    {
        ArgumentNullException.ThrowIfNull(meal);
        return new MealSearchResponse(meal.Id, meal.Name, meal.Type, meal.Kcal, meal.Protein);
    }
}

public sealed record MealDetailsResponse(
    Guid Id,
    string Name,
    string Type,
    int Kcal,
    int Protein,
    string Description,
    IReadOnlyList<MealDetailsIngredientResponse> Ingredients)
{
    public static MealDetailsResponse From(MealDetailsDto meal)
    {
        ArgumentNullException.ThrowIfNull(meal);
        return new MealDetailsResponse(
            meal.Id,
            meal.Name,
            meal.Type,
            meal.Kcal,
            meal.Protein,
            meal.Description,
            meal.Ingredients.Select(ingredient =>
                new MealDetailsIngredientResponse(
                    ingredient.Name,
                    ingredient.Quantity,
                    ingredient.Unit,
                    ingredient.Category)).ToArray());
    }
}

public sealed record MealDetailsIngredientResponse(string Name, decimal Quantity, string Unit, string Category);
