namespace DietPlanner.Mobile.Services;

public sealed class ShellUserPromptService : IUserPromptService
{
    public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
        => Shell.Current.DisplayAlert(title, message, accept, cancel);
}
