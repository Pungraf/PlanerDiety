using DietPlanner.Application.Abstractions;
using DietPlanner.Application.Shopping;

namespace DietPlanner.Application.Plans.Commands;

public sealed record CopyDayCommand(Guid UserId, DateOnly SourceDate, DateOnly TargetDate);

public sealed class CopyDayHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IShoppingListService _shoppingListService;

    public CopyDayHandler(IApplicationDbContext dbContext, IShoppingListService shoppingListService)
    {
        _dbContext = dbContext;
        _shoppingListService = shoppingListService;
    }

    public async Task<CopyDayResult?> HandleAsync(CopyDayCommand command, CancellationToken cancellationToken)
    {
        var plan = await _dbContext.FindReadableWeeklyPlanAsync(command.UserId, cancellationToken);
        if (plan is null)
        {
            return null;
        }

        if (!plan.CopyDay(command.SourceDate, command.TargetDate))
        {
            return null;
        }

        var mealIds = plan.Days
            .SelectMany(day => day.MealSlots)
            .Where(slot => slot.MealId.HasValue)
            .Select(slot => slot.MealId!.Value)
            .Distinct()
            .ToArray();

        var meals = await _dbContext.FindMealsByIdsAsync(mealIds, cancellationToken);
        var generatedShoppingList = _shoppingListService.GenerateForPlan(plan, meals);
        var existingShoppingList = await _dbContext.FindShoppingListByWeeklyPlanIdAsync(plan.Id, cancellationToken);

        if (existingShoppingList is null)
        {
            await _dbContext.AddShoppingListAsync(generatedShoppingList, cancellationToken);
        }
        else
        {
            existingShoppingList.ReplaceItems(generatedShoppingList.Items);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new CopyDayResult(command.SourceDate, command.TargetDate);
    }
}

public sealed record CopyDayResult(DateOnly SourceDate, DateOnly TargetDate);
