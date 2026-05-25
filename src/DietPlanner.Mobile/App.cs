namespace DietPlanner.Mobile;

public sealed class App : Application
{
    public App()
    {
        MainPage = new ContentPage
        {
            Content = new VerticalStackLayout
            {
                Children =
                {
                    new Label
                    {
                        HorizontalOptions = LayoutOptions.Center,
                        Text = "DietPlanner Mobile",
                        VerticalOptions = LayoutOptions.Center
                    }
                }
            }
        };
    }
}
