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
        meals[0].Name.Should().Be("Chicken Tomato Pasta");
        meals[0].Type.Should().Be("lunch");
        meals[0].Kcal.Should().Be(610);
        meals[0].Protein.Should().Be(35);
    }

    [Fact]
    public async Task SearchMeals_ShouldReturnBadRequest_WhenTypeIsAnUndefinedEnumValue()
    {
        await using var app = await DietPlanner.Api.Tests.Plans.PlansApiFactory.WithDraftPlanAsync();
        using var client = await app.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/meals?type=999");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed record MealSearchResponse(Guid Id, string Name, string Type, int Kcal, int Protein);
}
