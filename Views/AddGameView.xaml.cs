using BoardGamerApp.ViewModels;

namespace BoardGamerApp.Views;

public partial class AddGameView : ContentPage
{
    private readonly AddGameViewModel _viewModel;

    public AddGameView(AddGameViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await _viewModel.LoadGroupsAsync();
    }
}
