using BoardGamerApp.ViewModels;

namespace BoardGamerApp.Views;

public partial class AddGameView : ContentPage
{
    public AddGameView(AddGameViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}