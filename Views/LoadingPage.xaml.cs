using BoardGamerApp.ViewModels;

namespace BoardGamerApp.Views;

public partial class LoadingPage : ContentPage
{
    private readonly LoadingPageViewModel _viewModel;

    public LoadingPage(LoadingPageViewModel viewModel)
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