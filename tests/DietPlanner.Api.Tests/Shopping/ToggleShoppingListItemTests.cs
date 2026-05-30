using System.Net;
using System.Net.Http.Json;
using DietPlanner.Api.Tests.Plans;
using FluentAssertions;

namespace DietPlanner.Api.Tests.Shopping;

public class ToggleShoppingListItemTests
{
    [Fact]
    public async Task ToggleItem_ShouldMarkItemCheckedAndMoveItToBottom()
    {
        await using var app = await PlansApiFactory.WithShoppingListAsync();
        using var client = await app.CreateAuthenticatedClientAsync();
        var listId = (await app.ReadShoppingListAsync())!.Id;

        var response = await client.PostAsync($"/api/shopping-lists/{listId}/items/{TestData.ChickenShoppingListItemId}/toggle", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await response.Content.ReadFromJsonAsync<ShoppingListDetailsResponse>();
        list.Should().NotBeNull();
        list!.Items.Select(item => item.Id).Should().Equal(
            TestData.OatsShoppingListItemId,
            TestData.TomatoShoppingListItemId,
            TestData.ChickenShoppingListItemId);
        list.Items.Last().IsChecked.Should().BeTrue();
    }

    [Fact]
    public async Task ToggleItem_ShouldNotBypassLinkedListConflictOnPlanEdit()
    {
        await using var app = await PlansApiFactory.WithPlanAndShoppingListAsync(
            userId => PlansApiFactory.CreateActivePlan(
                userId,
                new DateOnly(2026, 5, 25),
                plan =>
                {
                    PlansApiFactory.AssignMeal(plan, new DateOnly(2026, 5, 26), DietPlanner.Domain.Enums.MealSlotType.Breakfast, TestData.BreakfastMealId);
                    PlansApiFactory.AssignMeal(plan, new DateOnly(2026, 5, 26), DietPlanner.Domain.Enums.MealSlotType.Dinner, TestData.DinnerMealId);
                }),
            plan =>
            {
                var shoppingList = new DietPlanner.Domain.Entities.ShoppingList(Guid.NewGuid(), plan.Id);
                shoppingList.ReplaceItems(
                [
                    new DietPlanner.Domain.Entities.ShoppingListItem(TestData.OatsShoppingListItemId, TestData.OatsIngredientId, 80m, "g"),
                    new DietPlanner.Domain.Entities.ShoppingListItem(TestData.TomatoSoupShoppingListItemId, TestData.TomatoIngredientId, 180m, "g")
                ]);

                return shoppingList;
            });
        using var client = await app.CreateAuthenticatedClientAsync();
        var listId = (await app.ReadShoppingListAsync())!.Id;

        var toggleResponse = await client.PostAsync($"/api/shopping-lists/{listId}/items/{TestData.OatsShoppingListItemId}/toggle", null);
        toggleResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var replaceResponse = await client.PutAsJsonAsync("/api/plans/current/days/2026-05-26/slots/dinner", new
        {
            MealId = TestData.LunchMealId,
            DeleteLinkedShoppingLists = false
        });

        replaceResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}

public sealed record ShoppingListDetailsResponse(Guid Id, string Name, IReadOnlyList<ShoppingListSummaryItemResponse> Items);

public sealed record ShoppingListSummaryItemResponse(Guid Id, string Name, decimal Quantity, string Unit, bool IsChecked);
