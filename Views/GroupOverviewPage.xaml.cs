using BoardGamerApp.ViewModels;

namespace BoardGamerApp.Views;

public partial class GroupOverviewPage : ContentPage
{

    private readonly GroupOverviewViewModel _viewModel;
    public GroupOverviewPage(GroupOverviewViewModel viewModel)
	{
		InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }


    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await _viewModel.LoadGroupsByPlayerIdAsync();
    }
}