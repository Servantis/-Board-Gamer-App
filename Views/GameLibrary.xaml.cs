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

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (!_viewModel.LoadGamesCommand.IsRunning)
        {
            _viewModel.LoadGamesCommand.Execute(null);
        }
    }
}