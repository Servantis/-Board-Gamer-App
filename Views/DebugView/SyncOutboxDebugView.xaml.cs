using BoardGamerApp.ViewModels;

namespace BoardGamerApp.Views;

public partial class SyncOutboxDebugView : ContentPage
{
    private readonly SyncOutboxDebugViewModel _viewModel;

    public SyncOutboxDebugView(SyncOutboxDebugViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await _viewModel.LoadAsync();
    }
}