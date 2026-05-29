namespace DietPlanner.Mobile.Services;

public static class GoogleSignInResultPolicy
{
    public static bool ShouldTreatAsCancellation(bool wasCanceled, bool hasIntentData)
        => wasCanceled && !hasIntentData;
}
