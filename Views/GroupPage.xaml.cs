using BoardGamerApp.Services.Interfaces;
using BoardGamerApp.ViewModels;
using System.Diagnostics;

namespace BoardGamerApp.Views;

public partial class GroupPage : ContentPage
{
	public GroupPage(GroupMembersViewModel viewModel)
	{
        InitializeComponent();

        BindingContext = viewModel;
#if DEBUG
        DebugTrigger.IsVisible = Debugger.IsAttached;
#else
        DebugFlyoutItem.IsVisible = false;

#endif
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
    }

}
