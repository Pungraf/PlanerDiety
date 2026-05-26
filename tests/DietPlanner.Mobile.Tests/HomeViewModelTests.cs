using DietPlanner.Mobile.Services;
using DietPlanner.Mobile.ViewModels;
using DietPlanner.Mobile.Navigation;
using Xunit;

namespace DietPlanner.Mobile.Tests;

public sealed class HomeViewModelTests
{
    [Fact]
    public async Task LoadCurrentDay_ShouldCalculateDailyKcalAndProteinTotals()
    {
        var plansClient = new FakePlansApiClient(
            new CurrentPlanDto(
                Guid.NewGuid(),
                "active",
                "2026-05-26",
                [
                    new PlanDayDto(
                        "2026-05-26",
                        [
                            new PlanMealSlotDto("breakfast", Guid.NewGuid(), "Oats Bowl", 500, 30),
                            new PlanMealSlotDto("lunch", Guid.NewGuid(), "Chicken Rice", 700, 45),
                            new PlanMealSlotDto("dinner", Guid.NewGuid(), "Salmon Potatoes", 600, 35)
                        ])
                ]));
        var viewModel = new HomeViewModel(plansClient, new RecordingNavigator(), new InMemoryMealSearchContextStore());

        await viewModel.LoadAsync();

        Assert.NotNull(viewModel.SelectedDay);
        Assert.Equal(1800, viewModel.SelectedDay!.TotalKcal);
        Assert.Equal(110, viewModel.SelectedDay.TotalProtein);
    }

    [Fact]
    public async Task CopySelectedDayToTarget_ShouldCallApiAndRefreshDays()
    {
        var sourceDate = new DateOnly(2026, 5, 26);
        var targetDate = new DateOnly(2026, 5, 27);
        var plansClient = new FakePlansApiClient(
            CreatePlan(
                sourceDate,
                [
                    new PlanMealSlotDto("breakfast", Guid.NewGuid(), "Oats Bowl", 500, 30),
                    new PlanMealSlotDto("lunch", Guid.NewGuid(), "Chicken Rice", 700, 45),
                    new PlanMealSlotDto("dinner", Guid.NewGuid(), "Salmon Potatoes", 600, 35)
                ],
                targetDate,
                [
                    new PlanMealSlotDto("breakfast", Guid.NewGuid(), "Toast", 250, 10)
                ]));
        var viewModel = new HomeViewModel(plansClient, new RecordingNavigator(), new InMemoryMealSearchContextStore());

        await viewModel.LoadAsync();
        var targetDay = Assert.Single(viewModel.Days, day => day.Date == targetDate);

        await viewModel.CopyDayCommand.ExecuteAsync(targetDay);

        Assert.Equal(sourceDate, plansClient.LastCopiedSourceDate);
        Assert.Equal(targetDate, plansClient.LastCopiedTargetDate);
        targetDay = Assert.Single(viewModel.Days, day => day.Date == targetDate);
        Assert.Equal(1800, targetDay.TotalKcal);
        Assert.Equal(110, targetDay.TotalProtein);
    }

    private static CurrentPlanDto CreatePlan(
        DateOnly sourceDate,
        IReadOnlyList<PlanMealSlotDto> sourceMeals,
        DateOnly targetDate,
        IReadOnlyList<PlanMealSlotDto> targetMeals)
    {
        return new CurrentPlanDto(
            Guid.NewGuid(),
            "active",
            sourceDate.ToString("yyyy-MM-dd"),
            [
                new PlanDayDto(sourceDate.ToString("yyyy-MM-dd"), sourceMeals),
                new PlanDayDto(targetDate.ToString("yyyy-MM-dd"), targetMeals)
            ]);
    }

    private sealed class FakePlansApiClient : IPlansApiClient
    {
        private CurrentPlanDto _plan;

        public FakePlansApiClient(CurrentPlanDto plan)
        {
            _plan = plan;
        }

        public DateOnly? LastCopiedSourceDate { get; private set; }

        public DateOnly? LastCopiedTargetDate { get; private set; }

        public Task CopyDayAsync(DateOnly sourceDate, DateOnly targetDate, CancellationToken cancellationToken = default)
        {
            LastCopiedSourceDate = sourceDate;
            LastCopiedTargetDate = targetDate;

            var sourceDay = _plan.Days.Single(day => day.Date == sourceDate.ToString("yyyy-MM-dd"));
            _plan = _plan with
            {
                Days = _plan.Days
                    .Select(day => day.Date == targetDate.ToString("yyyy-MM-dd")
                        ? day with { Meals = sourceDay.Meals.Select(meal => meal with { }).ToArray() }
                        : day)
                    .ToArray()
            };

            return Task.CompletedTask;
        }

        public Task<CurrentPlanDto> GetCurrentPlanAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_plan);
        }

        public Task<IReadOnlyList<MealSummaryDto>> SearchMealsAsync(string? query, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task ReplaceMealAsync(DateOnly date, string slotType, Guid mealId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class RecordingNavigator : IAppNavigator
    {
        public Task GoToAsync(string route)
        {
            return Task.CompletedTask;
        }
    }
}
