using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Globalization;
using DietPlanner.Application.Plans.Commands;
using DietPlanner.Application.Plans.Queries;
using DietPlanner.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DietPlanner.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/plans")]
public sealed class PlansController : ControllerBase
{
    private readonly GetCurrentPlanHandler _getCurrentPlanHandler;
    private readonly ActivateDraftHandler _activateDraftHandler;
    private readonly ReplaceMealHandler _replaceMealHandler;

    public PlansController(
        GetCurrentPlanHandler getCurrentPlanHandler,
        ActivateDraftHandler activateDraftHandler,
        ReplaceMealHandler replaceMealHandler)
    {
        _getCurrentPlanHandler = getCurrentPlanHandler;
        _activateDraftHandler = activateDraftHandler;
        _replaceMealHandler = replaceMealHandler;
    }

    [HttpGet("current")]
    public async Task<ActionResult<CurrentPlanResponse>> GetCurrent(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var plan = await _getCurrentPlanHandler.HandleAsync(new GetCurrentPlanQuery(userId.Value), cancellationToken);
        return plan is null ? NotFound() : Ok(CurrentPlanResponse.From(plan));
    }

    [HttpPost("current/activate")]
    public async Task<ActionResult<CurrentPlanResponse>> ActivateCurrentDraft(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var plan = await _activateDraftHandler.HandleAsync(new ActivateDraftCommand(userId.Value), cancellationToken);
        return plan is null ? NotFound() : Ok(CurrentPlanResponse.From(plan));
    }

    [HttpPut("current/days/{date}/slots/{slotType}")]
    public async Task<ActionResult<ReplaceMealResponse>> ReplaceMeal(
        string date,
        string slotType,
        [FromBody] ReplaceMealRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
        {
            return BadRequest();
        }

        if (!Enum.TryParse<MealSlotType>(slotType, true, out var parsedSlotType))
        {
            return BadRequest();
        }

        if (request is null || request.MealId == Guid.Empty)
        {
            return BadRequest();
        }

        var result = await _replaceMealHandler.HandleAsync(
            new ReplaceMealCommand(userId.Value, parsedDate, parsedSlotType, request.MealId),
            cancellationToken);

        return result is null ? NotFound() : Ok(ReplaceMealResponse.From(result));
    }

    private Guid? GetCurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return Guid.TryParse(subject, out var userId) ? userId : null;
    }
}

public sealed record ReplaceMealRequest(Guid MealId);

public sealed record ReplaceMealResponse(string Date, string SlotType, Guid MealId)
{
    public static ReplaceMealResponse From(ReplaceMealResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new ReplaceMealResponse(result.Date.ToString("yyyy-MM-dd"), result.SlotType.ToString().ToLowerInvariant(), result.MealId);
    }
}

public sealed record CurrentPlanResponse(Guid Id, string Status, string StartDate, string DinnerMode)
{
    public static CurrentPlanResponse From(CurrentPlanDto plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return new CurrentPlanResponse(plan.Id, plan.Status, plan.StartDate.ToString("yyyy-MM-dd"), plan.DinnerMode);
    }
}
