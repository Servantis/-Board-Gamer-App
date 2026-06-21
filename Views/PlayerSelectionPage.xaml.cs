using BoardGamerApp.ViewModels;

namespace BoardGamerApp.Views;

public partial class PlayerSelectionPage : ContentPage
{
    private readonly PlayerSelectionViewModel _viewModel;

    public PlayerSelectionPage(PlayerSelectionViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await _viewModel.InitializeAsync();
    }
}