using BoardGamerApp.ViewModels;

namespace BoardGamerApp.Views;

public partial class AddGamePage : ContentPage
{
    public AddGamePage(AddGameViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}