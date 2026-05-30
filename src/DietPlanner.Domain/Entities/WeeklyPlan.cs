using DietPlanner.Domain.Enums;

namespace DietPlanner.Domain.Entities;

public class WeeklyPlan
{
    private readonly List<DailyPlan> _days = [];

    public Guid Id { get; }

    public Guid UserId { get; private set; }

    public DateOnly StartDate { get; private set; }

    public DinnerMode DinnerMode { get; private set; }

    public WeeklyPlanStatus Status { get; private set; }

    public IReadOnlyCollection<DailyPlan> Days => _days;

    private WeeklyPlan(Guid id, Guid userId, DateOnly startDate, DinnerMode dinnerMode, WeeklyPlanStatus status)
    {
        Id = id;
        UserId = userId;
        StartDate = startDate;
        DinnerMode = dinnerMode;
        Status = status;
    }

    public static WeeklyPlan CreateDraft(Guid userId, DateOnly startDate, DinnerMode dinnerMode)
    {
        return new WeeklyPlan(
            Guid.NewGuid(),
            Guard.AgainstEmpty(userId, nameof(userId)),
            startDate,
            Guard.AgainstUndefinedEnum(dinnerMode, nameof(dinnerMode)),
            WeeklyPlanStatus.Draft);
    }

    public static WeeklyPlan CreateCurrent(Guid userId, DateOnly startDate, DinnerMode dinnerMode)
    {
        return new WeeklyPlan(
            Guid.NewGuid(),
            Guard.AgainstEmpty(userId, nameof(userId)),
            startDate,
            Guard.AgainstUndefinedEnum(dinnerMode, nameof(dinnerMode)),
            WeeklyPlanStatus.Current);
    }

    public static WeeklyPlan CreateFuture(Guid userId, DateOnly startDate, DinnerMode dinnerMode)
    {
        return new WeeklyPlan(
            Guid.NewGuid(),
            Guard.AgainstEmpty(userId, nameof(userId)),
            startDate,
            Guard.AgainstUndefinedEnum(dinnerMode, nameof(dinnerMode)),
            WeeklyPlanStatus.Future);
    }

    public void Activate()
    {
        if (Status != WeeklyPlanStatus.Draft)
        {
            throw new InvalidOperationException("Only draft plans can be activated.");
        }

        Status = WeeklyPlanStatus.Active;
    }

    public void PromoteFutureToCurrent()
    {
        if (Status != WeeklyPlanStatus.Future)
        {
            throw new InvalidOperationException("Only future plans can be promoted.");
        }

        Status = WeeklyPlanStatus.Current;
    }

    public void MarkDraftAsCurrent()
    {
        if (Status != WeeklyPlanStatus.Draft)
        {
            throw new InvalidOperationException("Only draft plans can be marked as current.");
        }

        Status = WeeklyPlanStatus.Current;
    }

    public void MarkDraftAsFuture()
    {
        if (Status != WeeklyPlanStatus.Draft)
        {
            throw new InvalidOperationException("Only draft plans can be marked as future.");
        }

        Status = WeeklyPlanStatus.Future;
    }

    public bool CopyDay(DateOnly sourceDate, DateOnly targetDate)
    {
        var sourceDay = _days.SingleOrDefault(day => day.Date == sourceDate);
        var targetDay = _days.SingleOrDefault(day => day.Date == targetDate);

        if (sourceDay is null || targetDay is null)
        {
            return false;
        }

        return targetDay.OverwriteMealsFrom(sourceDay);
    }

    internal void AddDay(DailyPlan day)
    {
        ArgumentNullException.ThrowIfNull(day);
        _days.Add(day);
    }
}
