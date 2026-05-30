using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Globalization;
using DietPlanner.Application.Plans.Commands;
using DietPlanner.Application.Plans.Planning;
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
    private readonly GetPlanningStateHandler _getPlanningStateHandler;
    private readonly GenerateFutureWeekHandler _generateFutureWeekHandler;
    private readonly ActivateDraftHandler _activateDraftHandler;
    private readonly ReplaceMealHandler _replaceMealHandler;
    private readonly CopyDayHandler _copyDayHandler;

    public PlansController(
        GetCurrentPlanHandler getCurrentPlanHandler,
        GetPlanningStateHandler getPlanningStateHandler,
        GenerateFutureWeekHandler generateFutureWeekHandler,
        ActivateDraftHandler activateDraftHandler,
        ReplaceMealHandler replaceMealHandler,
        CopyDayHandler copyDayHandler)
    {
        _getCurrentPlanHandler = getCurrentPlanHandler;
        _getPlanningStateHandler = getPlanningStateHandler;
        _generateFutureWeekHandler = generateFutureWeekHandler;
        _activateDraftHandler = activateDraftHandler;
        _replaceMealHandler = replaceMealHandler;
        _copyDayHandler = copyDayHandler;
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

    [HttpGet("state")]
    public async Task<ActionResult<PlanningStateResponse>> GetState(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var state = await _getPlanningStateHandler.HandleAsync(new GetPlanningStateQuery(userId.Value, today), cancellationToken);
        return state is null ? NotFound() : Ok(PlanningStateResponse.From(state));
    }

    [HttpPost("future/generate")]
    public async Task<ActionResult<PlanningStateResponse>> GenerateFutureWeek(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var state = await _generateFutureWeekHandler.HandleAsync(new GenerateFutureWeekCommand(userId.Value, today), cancellationToken);
        return state is null ? Conflict() : Ok(PlanningStateResponse.From(state));
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

        if (!Enum.IsDefined(parsedSlotType))
        {
            return BadRequest();
        }

        if (request is null || request.MealId == Guid.Empty)
        {
            return BadRequest();
        }

        var result = await _replaceMealHandler.HandleAsync(
            new ReplaceMealCommand(userId.Value, parsedDate, parsedSlotType, request.MealId, request.DeleteLinkedShoppingLists),
            cancellationToken);

        if (result is null || !result.Succeeded)
        {
            return result?.FailureReason == PlanEditFailureReason.LinkedShoppingListsExist
                ? Conflict(new { code = "linkedShoppingListsExist" })
                : NotFound();
        }

        return Ok(ReplaceMealResponse.From(result));
    }

    [HttpPost("current/copy-day")]
    public async Task<IActionResult> CopyDay([FromBody] CopyDayRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        if (request is null
            || !DateOnly.TryParseExact(request.SourceDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sourceDate)
            || !DateOnly.TryParseExact(request.TargetDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var targetDate))
        {
            return BadRequest();
        }

        var result = await _copyDayHandler.HandleAsync(
            new CopyDayCommand(userId.Value, sourceDate, targetDate, request.DeleteLinkedShoppingLists),
            cancellationToken);

        if (result is null || !result.Succeeded)
        {
            return result?.FailureReason == PlanEditFailureReason.LinkedShoppingListsExist
                ? Conflict(new { code = "linkedShoppingListsExist" })
                : NotFound();
        }

        return Ok();
    }

    private Guid? GetCurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return Guid.TryParse(subject, out var userId) ? userId : null;
    }
}

public sealed record ReplaceMealRequest(Guid MealId, bool DeleteLinkedShoppingLists = false);

public sealed record CopyDayRequest(string SourceDate, string TargetDate, bool DeleteLinkedShoppingLists = false);

public sealed record ReplaceMealResponse(string Date, string SlotType, Guid MealId)
{
    public static ReplaceMealResponse From(ReplaceMealResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new ReplaceMealResponse(result.Date!.Value.ToString("yyyy-MM-dd"), result.SlotType!.Value.ToString().ToLowerInvariant(), result.MealId!.Value);
    }
}

public sealed record CurrentPlanResponse(Guid Id, string Status, string StartDate, string DinnerMode, IReadOnlyList<CurrentPlanDayResponse> Days)
{
    public static CurrentPlanResponse From(CurrentPlanDto plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return new CurrentPlanResponse(
            plan.Id,
            plan.Status,
            plan.StartDate.ToString("yyyy-MM-dd"),
            plan.DinnerMode,
            plan.Days.Select(CurrentPlanDayResponse.From).ToArray());
    }
}

public sealed record PlanningStateResponse(CurrentPlanResponse CurrentPlan, CurrentPlanResponse? FuturePlan, bool CanGenerateFutureWeek)
{
    public static PlanningStateResponse From(PlanningStateDto state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return new PlanningStateResponse(
            CurrentPlanResponse.From(state.CurrentPlan),
            state.FuturePlan is null ? null : CurrentPlanResponse.From(state.FuturePlan),
            state.CanGenerateFutureWeek);
    }
}

public sealed record CurrentPlanDayResponse(string Date, IReadOnlyList<CurrentPlanMealSlotResponse> Meals)
{
    public static CurrentPlanDayResponse From(CurrentPlanDayDto day)
    {
        ArgumentNullException.ThrowIfNull(day);
        return new CurrentPlanDayResponse(
            day.Date.ToString("yyyy-MM-dd"),
            day.Meals.Select(CurrentPlanMealSlotResponse.From).ToArray());
    }
}

public sealed record CurrentPlanMealSlotResponse(string SlotType, Guid? MealId, string Name, int Kcal, int Protein)
{
    public static CurrentPlanMealSlotResponse From(CurrentPlanMealSlotDto slot)
    {
        ArgumentNullException.ThrowIfNull(slot);
        return new CurrentPlanMealSlotResponse(slot.SlotType, slot.MealId, slot.Name, slot.Kcal, slot.Protein);
    }
}
