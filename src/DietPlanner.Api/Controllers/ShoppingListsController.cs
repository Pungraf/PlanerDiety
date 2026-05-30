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
    private readonly ListShoppingListsHandler _listShoppingListsHandler;
    private readonly CreateShoppingListHandler _createShoppingListHandler;
    private readonly GetShoppingListDetailsHandler _getShoppingListDetailsHandler;
    private readonly GetShoppingListCreateOptionsHandler _getShoppingListCreateOptionsHandler;
    private readonly DeleteShoppingListHandler _deleteShoppingListHandler;
    private readonly GetShoppingListHandler _getShoppingListHandler;
    private readonly ToggleShoppingListItemHandler _toggleShoppingListItemHandler;

    public ShoppingListsController(
        ListShoppingListsHandler listShoppingListsHandler,
        CreateShoppingListHandler createShoppingListHandler,
        GetShoppingListDetailsHandler getShoppingListDetailsHandler,
        GetShoppingListCreateOptionsHandler getShoppingListCreateOptionsHandler,
        DeleteShoppingListHandler deleteShoppingListHandler,
        GetShoppingListHandler getShoppingListHandler,
        ToggleShoppingListItemHandler toggleShoppingListItemHandler)
    {
        _listShoppingListsHandler = listShoppingListsHandler;
        _createShoppingListHandler = createShoppingListHandler;
        _getShoppingListDetailsHandler = getShoppingListDetailsHandler;
        _getShoppingListCreateOptionsHandler = getShoppingListCreateOptionsHandler;
        _deleteShoppingListHandler = deleteShoppingListHandler;
        _getShoppingListHandler = getShoppingListHandler;
        _toggleShoppingListItemHandler = toggleShoppingListItemHandler;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ShoppingListSummaryResponse>>> List([FromQuery] Guid? planId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var shoppingLists = await _listShoppingListsHandler.HandleAsync(
            new ListShoppingListsQuery(userId.Value, planId),
            cancellationToken);

        return Ok(shoppingLists.Select(ShoppingListSummaryResponse.From).ToArray());
    }

    [HttpPost]
    public async Task<ActionResult<ShoppingListDetailsResponse>> Create(
        [FromQuery] Guid? planId,
        [FromBody] CreateShoppingListRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        ArgumentNullException.ThrowIfNull(request);

        var shoppingList = await _createShoppingListHandler.HandleAsync(
            new CreateShoppingListCommand(
                userId.Value,
                planId,
                request.Name,
                request.IngredientKeys.Select(ingredient => ingredient.ToCommand()).ToArray()),
            cancellationToken);

        return shoppingList is null
            ? NotFound()
            : CreatedAtAction(nameof(GetCurrent), new { }, ShoppingListDetailsResponse.From(shoppingList));
    }

    [HttpGet("{listId:guid}")]
    public async Task<ActionResult<ShoppingListDetailsResponse>> GetDetails(Guid listId, [FromQuery] Guid? planId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var shoppingList = await _getShoppingListDetailsHandler.HandleAsync(
            new GetShoppingListDetailsQuery(userId.Value, listId, planId),
            cancellationToken);

        return shoppingList is null ? NotFound() : Ok(ShoppingListDetailsResponse.From(shoppingList));
    }

    [HttpGet("create-options")]
    public async Task<ActionResult<IReadOnlyList<ShoppingListCreateDayResponse>>> GetCreateOptions([FromQuery] Guid? planId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var options = await _getShoppingListCreateOptionsHandler.HandleAsync(
            new GetShoppingListCreateOptionsQuery(userId.Value, planId),
            cancellationToken);

        return Ok(options.Select(ShoppingListCreateDayResponse.From).ToArray());
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

    [HttpDelete("{listId:guid}")]
    public async Task<IActionResult> Delete(Guid listId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var deleted = await _deleteShoppingListHandler.HandleAsync(
            new DeleteShoppingListCommand(userId.Value, listId),
            cancellationToken);

        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("{listId:guid}/items/{itemId:guid}/toggle")]
    public async Task<ActionResult<ShoppingListDetailsResponse>> ToggleItem(Guid listId, Guid itemId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        if (listId == Guid.Empty || itemId == Guid.Empty)
        {
            return BadRequest();
        }

        var shoppingList = await _toggleShoppingListItemHandler.HandleAsync(
            new ToggleShoppingListItemCommand(userId.Value, listId, itemId),
            cancellationToken);

        return shoppingList is null ? NotFound() : Ok(ShoppingListDetailsResponse.From(shoppingList));
    }

    private Guid? GetCurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return Guid.TryParse(subject, out var userId) ? userId : null;
    }
}

public sealed record CreateShoppingListRequest(string Name, IReadOnlyList<SelectedShoppingIngredientRequest> IngredientKeys);

public sealed record SelectedShoppingIngredientRequest(string Date, string SlotType, Guid IngredientId)
{
    public SelectedShoppingIngredient ToCommand()
    {
        if (!DateOnly.TryParse(Date, out var date))
        {
            throw new ArgumentException("Date must be a valid ISO date.", nameof(Date));
        }

        if (!Enum.TryParse<Domain.Enums.MealSlotType>(SlotType, true, out var slotType))
        {
            throw new ArgumentException("SlotType is invalid.", nameof(SlotType));
        }

        return new SelectedShoppingIngredient(date, slotType, IngredientId);
    }
}

public sealed record ShoppingListSummaryResponse(Guid Id, string Name, string CreatedAt, int ItemCount)
{
    public static ShoppingListSummaryResponse From(ShoppingListSummaryDto shoppingList)
    {
        ArgumentNullException.ThrowIfNull(shoppingList);
        return new ShoppingListSummaryResponse(
            shoppingList.Id,
            shoppingList.Name,
            shoppingList.CreatedAt.ToString("O"),
            shoppingList.ItemCount);
    }
}

public sealed record ShoppingListDetailsResponse(Guid Id, string Name, IReadOnlyList<ShoppingListSummaryItemResponse> Items)
{
    public static ShoppingListDetailsResponse From(ShoppingListDetailsDto shoppingList)
    {
        ArgumentNullException.ThrowIfNull(shoppingList);
        return new ShoppingListDetailsResponse(
            shoppingList.Id,
            shoppingList.Name,
            shoppingList.Items.Select(ShoppingListSummaryItemResponse.From).ToArray());
    }
}

public sealed record ShoppingListCreateDayResponse(string Date, IReadOnlyList<ShoppingListCreateMealResponse> Meals)
{
    public static ShoppingListCreateDayResponse From(ShoppingListCreateDayDto day)
    {
        ArgumentNullException.ThrowIfNull(day);
        return new ShoppingListCreateDayResponse(day.Date, day.Meals.Select(ShoppingListCreateMealResponse.From).ToArray());
    }
}

public sealed record ShoppingListCreateMealResponse(string SlotType, string MealName, IReadOnlyList<ShoppingListCreateIngredientResponse> Ingredients)
{
    public static ShoppingListCreateMealResponse From(ShoppingListCreateMealDto meal)
    {
        ArgumentNullException.ThrowIfNull(meal);
        return new ShoppingListCreateMealResponse(meal.SlotType, meal.MealName, meal.Ingredients.Select(ShoppingListCreateIngredientResponse.From).ToArray());
    }
}

public sealed record ShoppingListCreateIngredientResponse(Guid IngredientId, string Name, decimal Quantity, string Unit, string Category)
{
    public static ShoppingListCreateIngredientResponse From(ShoppingListCreateIngredientDto ingredient)
    {
        ArgumentNullException.ThrowIfNull(ingredient);
        return new ShoppingListCreateIngredientResponse(ingredient.IngredientId, ingredient.Name, ingredient.Quantity, ingredient.Unit, ingredient.Category);
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
