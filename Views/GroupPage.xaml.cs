using BoardGamerApp.Services.Interfaces;
using BoardGamerApp.ViewModels;

namespace BoardGamerApp.Views;

public partial class GroupPage : ContentPage
{
	public GroupPage(GroupMembersViewModel viewModel)
	{
        InitializeComponent();

        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
    }
}