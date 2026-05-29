using DietPlanner.Mobile.Services;
using Xunit;

namespace DietPlanner.Mobile.Tests;

public sealed class GoogleSignInResultPolicyTests
{
    [Fact]
    public void ShouldTreatAsCancellation_WhenCanceledWithoutIntentData_ShouldBeTrue()
    {
        var result = GoogleSignInResultPolicy.ShouldTreatAsCancellation(wasCanceled: true, hasIntentData: false);

        Assert.True(result);
    }

    [Fact]
    public void ShouldTreatAsCancellation_WhenCanceledWithIntentData_ShouldBeFalse()
    {
        var result = GoogleSignInResultPolicy.ShouldTreatAsCancellation(wasCanceled: true, hasIntentData: true);

        Assert.False(result);
    }
}
