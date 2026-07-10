using BoardGamerApp.ViewModels;
using Microsoft.Maui.Controls;

namespace BoardGamerApp.Views;

[QueryProperty(nameof(GroupId), "groupId")]
public partial class GroupManagementPage : ContentPage
{
    public GroupManagementPage(GroupMembersViewModel viewModel)
    {
        InitializeComponent();

        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is GroupMembersViewModel vm)
        {
            await vm.RefreshAsync();
        }
    }

    public string GroupId
    {
        set
        {
            if (BindingContext is GroupMembersViewModel vm)
            {
                vm.GroupId = value;
            }
        }
    }
}