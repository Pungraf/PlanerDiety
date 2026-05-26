using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DietPlanner.Application.Shopping.Commands;
using DietPlanner.Application.Shopping.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DietPlanner.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/shopping-lists")]
public sealed class ShoppingListsController : ControllerBase
{
    private readonly GetShoppingListHandler _getShoppingListHandler;
    private readonly ToggleShoppingListItemHandler _toggleShoppingListItemHandler;

    public ShoppingListsController(
        GetShoppingListHandler getShoppingListHandler,
        ToggleShoppingListItemHandler toggleShoppingListItemHandler)
    {
        _getShoppingListHandler = getShoppingListHandler;
        _toggleShoppingListItemHandler = toggleShoppingListItemHandler;
    }

    [HttpGet("current")]
    public async Task<ActionResult<ShoppingListResponse>> GetCurrent(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var shoppingList = await _getShoppingListHandler.HandleAsync(new GetShoppingListQuery(userId.Value), cancellationToken);
        return shoppingList is null ? NotFound() : Ok(ShoppingListResponse.From(shoppingList));
    }

    [HttpPost("current/items/{itemId:guid}/toggle")]
    public async Task<ActionResult<ShoppingListResponse>> ToggleItem(Guid itemId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        if (itemId == Guid.Empty)
        {
            return BadRequest();
        }

        var shoppingList = await _toggleShoppingListItemHandler.HandleAsync(
            new ToggleShoppingListItemCommand(userId.Value, itemId),
            cancellationToken);

        return shoppingList is null ? NotFound() : Ok(ShoppingListResponse.From(shoppingList));
    }

    private Guid? GetCurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return Guid.TryParse(subject, out var userId) ? userId : null;
    }
}

public sealed record ShoppingListResponse(Guid Id, IReadOnlyList<ShoppingListSummaryItemResponse> SummaryItems)
{
    public static ShoppingListResponse From(ShoppingListDto shoppingList)
    {
        ArgumentNullException.ThrowIfNull(shoppingList);
        return new ShoppingListResponse(shoppingList.Id, shoppingList.SummaryItems.Select(ShoppingListSummaryItemResponse.From).ToArray());
    }
}

public sealed record ShoppingListSummaryItemResponse(Guid Id, string Name, decimal Quantity, string Unit, bool IsChecked)
{
    public static ShoppingListSummaryItemResponse From(ShoppingListSummaryItemDto item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return new ShoppingListSummaryItemResponse(item.Id, item.Name, item.Quantity, item.Unit, item.IsChecked);
    }
}
