using DietPlanner.Domain.Enums;

namespace DietPlanner.Domain.Entities;

public class WeeklyPlan
{
    private readonly List<DailyPlan> _days = [];

    public Guid Id { get; init; }

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
        return new WeeklyPlan(Guid.NewGuid(), userId, startDate, dinnerMode, WeeklyPlanStatus.Draft);
    }

    public void Activate()
    {
        if (Status != WeeklyPlanStatus.Draft)
        {
            throw new InvalidOperationException("Only draft plans can be activated.");
        }

        Status = WeeklyPlanStatus.Active;
    }
}
