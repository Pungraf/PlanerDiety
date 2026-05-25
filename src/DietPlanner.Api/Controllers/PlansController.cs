using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DietPlanner.Application.Plans.Commands;
using DietPlanner.Application.Plans.Queries;
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

    public PlansController(GetCurrentPlanHandler getCurrentPlanHandler, ActivateDraftHandler activateDraftHandler)
    {
        _getCurrentPlanHandler = getCurrentPlanHandler;
        _activateDraftHandler = activateDraftHandler;
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

    private Guid? GetCurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return Guid.TryParse(subject, out var userId) ? userId : null;
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
