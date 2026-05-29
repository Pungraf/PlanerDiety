using System.Net;
using System.Net.Http.Json;
using DietPlanner.Api.Tests.Plans;
using FluentAssertions;

namespace DietPlanner.Api.Tests.Meals;

public class GetMealDetailsTests
{
    [Fact]
    public async Task GetMealDetails_ShouldReturnMealMacrosAndIngredients()
    {
        await using var app = await PlansApiFactory.WithDraftPlanAsync();
        using var client = await app.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/meals/{TestData.BreakfastMealId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<MealDetailsResponse>();
        payload.Should().NotBeNull();
        payload!.Id.Should().Be(TestData.BreakfastMealId);
        payload.Name.Should().Be("Berry Oat Bowl");
        payload.Type.Should().Be("breakfast");
        payload.Kcal.Should().Be(420);
        payload.Protein.Should().Be(18);
        payload.Description.Should().Be("Stir oats with yogurt and berries.");
        payload.Ingredients.Should().ContainSingle(x =>
            x.Name == "Oats" &&
            x.Quantity == 80m &&
            x.Unit == "g" &&
            x.Category == "Pantry");
    }

    [Fact]
    public async Task GetMealDetails_ShouldReturnNotFound_WhenMealDoesNotExist()
    {
        await using var app = await PlansApiFactory.WithDraftPlanAsync();
        using var client = await app.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/meals/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record MealDetailsResponse(
        Guid Id,
        string Name,
        string Type,
        int Kcal,
        int Protein,
        string Description,
        IReadOnlyList<MealIngredientResponse> Ingredients);

    private sealed record MealIngredientResponse(string Name, decimal Quantity, string Unit, string Category);
}
