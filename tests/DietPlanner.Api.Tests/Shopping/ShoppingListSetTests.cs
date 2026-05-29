using System.Net;
using System.Net.Http.Json;
using DietPlanner.Api.Tests.Plans;
using DietPlanner.Domain.Enums;
using FluentAssertions;

namespace DietPlanner.Api.Tests.Shopping;

public sealed class ShoppingListSetTests
{
    [Fact]
    public async Task CreateShoppingList_ShouldSummarizeSelectedIngredientsForCurrentWeek()
    {
        await using var app = await PlansApiFactory.WithActivePlanAsync(userId =>
            PlansApiFactory.CreateActivePlan(userId, new DateOnly(2026, 5, 25), plan =>
            {
                PlansApiFactory.AssignMeal(plan, new DateOnly(2026, 5, 25), MealSlotType.Breakfast, TestData.BreakfastMealId);
                PlansApiFactory.AssignMeal(plan, new DateOnly(2026, 5, 25), MealSlotType.Dinner, TestData.LunchMealId);
            }));
        using var client = await app.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/shopping-lists", new
        {
            name = "Weekly shop",
            ingredientKeys = new[]
            {
                new { date = "2026-05-25", slotType = "breakfast", ingredientId = TestData.OatsIngredientId },
                new { date = "2026-05-25", slotType = "dinner", ingredientId = TestData.ChickenIngredientId }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var payload = await response.Content.ReadFromJsonAsync<ShoppingListDetailsResponse>();
        payload.Should().NotBeNull();
        payload!.Name.Should().Be("Weekly shop");
        payload.Items.Should().Contain(x => x.Name == "Oats" && x.Quantity == 80m);
        payload.Items.Should().Contain(x => x.Name == "Chicken" && x.Quantity == 140m);
    }

    [Fact]
    public async Task ListShoppingLists_ShouldReturnAllListsForCurrentWeek()
    {
        await using var app = await PlansApiFactory.WithShoppingListAsync();
        using var client = await app.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/shopping-lists");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, body);

        var payload = await response.Content.ReadFromJsonAsync<IReadOnlyList<ShoppingListSummaryResponse>>();
        payload.Should().NotBeNull();
        payload!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetShoppingListDetails_ShouldReturnItemsForSelectedList()
    {
        await using var app = await PlansApiFactory.WithShoppingListAsync();
        using var client = await app.CreateAuthenticatedClientAsync();
        var listId = (await app.ReadShoppingListAsync())!.Id;

        var response = await client.GetAsync($"/api/shopping-lists/{listId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ShoppingListDetailsResponse>();
        payload.Should().NotBeNull();
        payload!.Items.Should().Contain(x => x.Name == "Chicken" && x.Quantity == 140m);
    }

    [Fact]
    public async Task DeleteShoppingList_ShouldRemoveSelectedList()
    {
        await using var app = await PlansApiFactory.WithShoppingListAsync();
        using var client = await app.CreateAuthenticatedClientAsync();
        var listId = (await app.ReadShoppingListAsync())!.Id;

        var response = await client.DeleteAsync($"/api/shopping-lists/{listId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await app.ReadShoppingListsAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task GetCreateOptions_ShouldReturnDaysMealsAndPositiveQuantityIngredients()
    {
        await using var app = await PlansApiFactory.WithActivePlanAsync(userId =>
            PlansApiFactory.CreateActivePlan(userId, new DateOnly(2026, 5, 25), plan =>
            {
                PlansApiFactory.AssignMeal(plan, new DateOnly(2026, 5, 25), MealSlotType.Breakfast, TestData.BreakfastMealId);
                PlansApiFactory.AssignMeal(plan, new DateOnly(2026, 5, 25), MealSlotType.Dinner, TestData.LunchMealId);
            }));
        using var client = await app.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/shopping-lists/create-options");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<IReadOnlyList<ShoppingListCreateDayResponse>>();
        payload.Should().NotBeNull();
        var day = payload!.Single(entry => entry.Date == "2026-05-25");
        day.Meals.Should().Contain(meal => meal.SlotType == "breakfast" && meal.Ingredients.Any(i => i.Name == "Oats" && i.Quantity == 80m));
    }

    private sealed record ShoppingListSummaryResponse(Guid Id, string Name, string CreatedAt, int ItemCount);

    private sealed record ShoppingListDetailsResponse(Guid Id, string Name, IReadOnlyList<ShoppingListItemResponse> Items);

    private sealed record ShoppingListItemResponse(Guid Id, string Name, decimal Quantity, string Unit, bool IsChecked);

    private sealed record ShoppingListCreateDayResponse(string Date, IReadOnlyList<ShoppingListCreateMealResponse> Meals);

    private sealed record ShoppingListCreateMealResponse(string SlotType, string MealName, IReadOnlyList<ShoppingListCreateIngredientResponse> Ingredients);

    private sealed record ShoppingListCreateIngredientResponse(Guid IngredientId, string Name, decimal Quantity, string Unit, string Category);
}
