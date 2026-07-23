using BoardGamerApp.ViewModels;

namespace BoardGamerApp.Views;

public partial class GameLibrary : ContentPage
{
    private readonly GameLibraryViewModel _viewModel;

    public GameLibrary(GameLibraryViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await _viewModel.LoadGamesAsync();
    }
}
