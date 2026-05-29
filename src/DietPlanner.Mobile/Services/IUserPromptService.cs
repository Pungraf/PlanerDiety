namespace DietPlanner.Mobile.Services;

public interface IUserPromptService
{
    Task<bool> ConfirmAsync(string title, string message, string accept, string cancel);
}
