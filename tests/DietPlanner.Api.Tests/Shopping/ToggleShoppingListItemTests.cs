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

        var response = await client.PostAsync($"/api/shopping-lists/current/items/{TestData.ChickenShoppingListItemId}/toggle", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await response.Content.ReadFromJsonAsync<ShoppingListResponse>();
        list.Should().NotBeNull();
        list!.SummaryItems.Select(item => item.Id).Should().Equal(
            TestData.OatsShoppingListItemId,
            TestData.TomatoShoppingListItemId,
            TestData.ChickenShoppingListItemId);
        list.SummaryItems.Last().IsChecked.Should().BeTrue();
    }
}

public sealed record ShoppingListResponse(Guid Id, IReadOnlyList<ShoppingListSummaryItemResponse> SummaryItems);

public sealed record ShoppingListSummaryItemResponse(Guid Id, string Name, decimal Quantity, string Unit, bool IsChecked);
