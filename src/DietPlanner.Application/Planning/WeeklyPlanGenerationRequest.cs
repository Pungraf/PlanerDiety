using DietPlanner.Domain.Entities;
using DietPlanner.Domain.Enums;

namespace DietPlanner.Application.Planning;

public sealed record WeeklyPlanGenerationRequest(
    Guid UserId,
    DateOnly StartDate,
    DinnerMode DinnerMode,
    IReadOnlyCollection<Meal> Meals);
