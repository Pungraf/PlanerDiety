using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace DietPlanner.Api.Tests.Meals;

public class SearchMealsTests
{
    [Fact]
    public async Task SearchMeals_ShouldFilterByNameTypeAndIngredient()
    {
        await using var app = await DietPlanner.Api.Tests.Plans.PlansApiFactory.WithDraftPlanAsync();
        using var client = await app.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/meals?name=Tomato&type=lunch&ingredient=Chicken");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<IReadOnlyList<MealSearchResponse>>();
        payload.Should().NotBeNull();
        var meals = payload!;
        meals.Select(x => x.Id).Should().Equal(TestData.LunchMealId);
        meals[0].Type.Should().Be("lunch");
    }

    private sealed record MealSearchResponse(Guid Id, string Name, string Type);
}
