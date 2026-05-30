using DietPlanner.Application.Abstractions;
using DietPlanner.Application.Planning;
using DietPlanner.Application.Plans.Queries;
using DietPlanner.Application.Shopping.Commands;
using DietPlanner.Domain.Entities;
using DietPlanner.Domain.Enums;

namespace DietPlanner.Application.Plans.Planning;

public sealed class PlanningStateService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IWeeklyPlanGenerator _weeklyPlanGenerator;
    private readonly DeleteShoppingListsForPlanHandler _deleteShoppingListsForPlanHandler;

    public PlanningStateService(
        IApplicationDbContext dbContext,
        IWeeklyPlanGenerator weeklyPlanGenerator,
        DeleteShoppingListsForPlanHandler deleteShoppingListsForPlanHandler)
    {
        _dbContext = dbContext;
        _weeklyPlanGenerator = weeklyPlanGenerator;
        _deleteShoppingListsForPlanHandler = deleteShoppingListsForPlanHandler;
    }

    public async Task<PlanningStateDto?> GetAsync(Guid userId, DateOnly today, CancellationToken cancellationToken)
    {
        var current = await _dbContext.FindCurrentWeeklyPlanAsync(userId, cancellationToken);
        var future = await _dbContext.FindFutureWeeklyPlanAsync(userId, cancellationToken);

        if (today.DayOfWeek == DayOfWeek.Sunday)
        {
            (current, future) = await RollForwardIfNeededAsync(userId, today, current, future, cancellationToken);
        }

        if (current is null)
        {
            current = await BootstrapCurrentAsync(userId, today, cancellationToken);
            future = await _dbContext.FindFutureWeeklyPlanAsync(userId, cancellationToken);
        }

        return current is null
            ? null
            : await BuildStateAsync(today, current, future, cancellationToken);
    }

    public async Task<PlanningStateDto?> GenerateFutureAsync(Guid userId, DateOnly today, CancellationToken cancellationToken)
    {
        var current = await _dbContext.FindCurrentWeeklyPlanAsync(userId, cancellationToken);
        var future = await _dbContext.FindFutureWeeklyPlanAsync(userId, cancellationToken);

        if (today.DayOfWeek == DayOfWeek.Sunday)
        {
            (current, future) = await RollForwardIfNeededAsync(userId, today, current, future, cancellationToken);
        }

        if (current is null)
        {
            current = await BootstrapCurrentAsync(userId, today, cancellationToken);
        }

        if (current is null || future is not null || !WeekBoundaryCalculator.CanGenerateFutureWeek(today, hasFuturePlan: false))
        {
            return null;
        }

        future = await GeneratePlanAsync(userId, WeekBoundaryCalculator.GetNextWeekStart(today), WeeklyPlanStatus.Future, cancellationToken);
        return await BuildStateAsync(today, current, future, cancellationToken);
    }

    private async Task<(WeeklyPlan? Current, WeeklyPlan? Future)> RollForwardIfNeededAsync(
        Guid userId,
        DateOnly today,
        WeeklyPlan? current,
        WeeklyPlan? future,
        CancellationToken cancellationToken)
    {
        var currentWeekStart = WeekBoundaryCalculator.GetWeekStart(today);
        if (current is not null && current.StartDate == currentWeekStart && current.Status == WeeklyPlanStatus.Current)
        {
            return (current, future);
        }

        if (current is not null)
        {
            await _deleteShoppingListsForPlanHandler.DeleteAsync(new DeleteShoppingListsForPlanCommand(current.Id), cancellationToken);
            _dbContext.RemoveWeeklyPlan(current);
        }

        if (future is not null)
        {
            if (future.Status == WeeklyPlanStatus.Draft)
            {
                future.MarkDraftAsCurrent();
            }
            else if (future.Status == WeeklyPlanStatus.Future)
            {
                future.PromoteFutureToCurrent();
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return (future, null);
        }

        current = await GeneratePlanAsync(userId, currentWeekStart, WeeklyPlanStatus.Current, cancellationToken);
        return (current, null);
    }

    private async Task<WeeklyPlan?> BootstrapCurrentAsync(Guid userId, DateOnly today, CancellationToken cancellationToken)
    {
        return await GeneratePlanAsync(userId, WeekBoundaryCalculator.GetWeekStart(today), WeeklyPlanStatus.Current, cancellationToken);
    }

    private async Task<WeeklyPlan?> GeneratePlanAsync(
        Guid userId,
        DateOnly startDate,
        WeeklyPlanStatus targetStatus,
        CancellationToken cancellationToken)
    {
        var meals = await _dbContext.SearchMealsAsync(null, null, null, cancellationToken);
        if (meals.Count == 0)
        {
            return null;
        }

        var plan = _weeklyPlanGenerator.Generate(new WeeklyPlanGenerationRequest(
            userId,
            startDate,
            DinnerMode.BreakfastStyle,
            meals));

        if (targetStatus == WeeklyPlanStatus.Current)
        {
            plan.MarkDraftAsCurrent();
        }
        else if (targetStatus == WeeklyPlanStatus.Future)
        {
            plan.MarkDraftAsFuture();
        }
        else
        {
            throw new InvalidOperationException("Unsupported target weekly plan status.");
        }

        await _dbContext.AddWeeklyPlanAsync(plan, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return plan;
    }

    private async Task<PlanningStateDto> BuildStateAsync(
        DateOnly today,
        WeeklyPlan current,
        WeeklyPlan? future,
        CancellationToken cancellationToken)
    {
        var mealIds = new[] { current, future }
            .Where(plan => plan is not null)
            .SelectMany(plan => plan!.Days)
            .SelectMany(day => day.MealSlots)
            .Where(slot => slot.MealId.HasValue)
            .Select(slot => slot.MealId!.Value)
            .Distinct()
            .ToArray();

        var meals = await _dbContext.FindMealsByIdsAsync(mealIds, cancellationToken);

        return new PlanningStateDto(
            CurrentPlanDto.From(current, meals),
            future is null ? null : CurrentPlanDto.From(future, meals),
            WeekBoundaryCalculator.CanGenerateFutureWeek(today, future is not null));
    }
}
