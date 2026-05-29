using DietPlanner.Mobile.Services;
using DietPlanner.Mobile.ViewModels;
using Xunit;

namespace DietPlanner.Mobile.Tests;

public sealed class MealDetailsViewModelTests
{
    [Fact]
    public async Task LoadAsync_ShouldLoadMealDetailsFromContext()
    {
        var mealId = Guid.NewGuid();
        var context = new InMemoryMealDetailsContextStore
        {
            Current = new MealDetailsContext(mealId)
        };
        var api = new FakePlansApiClient(new MealDetailsDto(mealId, "Owsianka", "breakfast", 450, 25, "Gotuj.", []));
        var viewModel = new MealDetailsViewModel(api, context);

        await viewModel.LoadAsync();

        Assert.Equal("Owsianka", viewModel.Name);
        Assert.Equal("Gotuj.", viewModel.Description);
    }

    private sealed class FakePlansApiClient : IPlansApiClient
    {
        private readonly MealDetailsDto _details;

        public FakePlansApiClient(MealDetailsDto details)
        {
            _details = details;
        }

        public Task CopyDayAsync(DateOnly sourceDate, DateOnly targetDate, bool deleteLinkedShoppingLists, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<CurrentPlanDto> GetCurrentPlanAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<MealSummaryDto>> SearchMealsAsync(string? query, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<MealSummaryDto>> GetMealCatalogAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<MealDetailsDto> GetMealDetailsAsync(Guid mealId, CancellationToken cancellationToken = default)
            => Task.FromResult(_details);

        public Task ReplaceMealAsync(DateOnly date, string slotType, Guid mealId, bool deleteLinkedShoppingLists, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
