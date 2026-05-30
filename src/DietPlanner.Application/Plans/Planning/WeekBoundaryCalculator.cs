namespace DietPlanner.Application.Plans.Planning;

public static class WeekBoundaryCalculator
{
    public static DateOnly GetWeekStart(DateOnly date)
    {
        return date.AddDays(-(int)date.DayOfWeek);
    }

    public static DateOnly GetNextWeekStart(DateOnly date)
    {
        return GetWeekStart(date).AddDays(7);
    }

    public static bool CanGenerateFutureWeek(DateOnly today, bool hasFuturePlan)
    {
        return !hasFuturePlan && today.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday;
    }
}
