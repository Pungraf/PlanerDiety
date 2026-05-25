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

    public MealsController(SearchMealsHandler searchMealsHandler)
    {
        _searchMealsHandler = searchMealsHandler;
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
