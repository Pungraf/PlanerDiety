using System.Windows.Input;
using DietPlanner.Mobile.Navigation;

namespace DietPlanner.Mobile.Views.Controls;

public partial class MainBottomNav : ContentView
{
    public static readonly BindableProperty ActiveTabProperty = BindableProperty.Create(
        nameof(ActiveTab),
        typeof(MainAppTab),
        typeof(MainBottomNav),
        MainAppTab.Home);

    public static readonly BindableProperty GoToHomeCommandProperty = BindableProperty.Create(
        nameof(GoToHomeCommand),
        typeof(ICommand),
        typeof(MainBottomNav));

    public static readonly BindableProperty GoToShoppingListsCommandProperty = BindableProperty.Create(
        nameof(GoToShoppingListsCommand),
        typeof(ICommand),
        typeof(MainBottomNav));

    public MainBottomNav()
    {
        InitializeComponent();
    }

    public MainAppTab ActiveTab
    {
        get => (MainAppTab)GetValue(ActiveTabProperty);
        set => SetValue(ActiveTabProperty, value);
    }

    public ICommand? GoToHomeCommand
    {
        get => (ICommand?)GetValue(GoToHomeCommandProperty);
        set => SetValue(GoToHomeCommandProperty, value);
    }

    public ICommand? GoToShoppingListsCommand
    {
        get => (ICommand?)GetValue(GoToShoppingListsCommandProperty);
        set => SetValue(GoToShoppingListsCommandProperty, value);
    }
}
