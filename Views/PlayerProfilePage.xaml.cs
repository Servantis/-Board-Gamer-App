using BoardGamerApp.ViewModels;

namespace BoardGamerApp.Views;

public partial class PlayerProfilePage : ContentPage
{
    public PlayerProfilePage(PlayerProfileViewModel viewModel)
    {
        InitializeComponent();

        BindingContext = viewModel;
    }
}