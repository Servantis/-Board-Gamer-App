using BoardGamerApp.Services.Interfaces;
using BoardGamerApp.ViewModels;

namespace BoardGamerApp.Views;

public partial class GroupPage : ContentPage
{
	public GroupPage(GroupMembersViewModel viewModel)
	{
        InitializeComponent();

        System.Diagnostics.Debug.WriteLine(
    $"GroupPage VM Hash: {viewModel.GetHashCode()}");

        System.Diagnostics.Debug.WriteLine(
            $"GroupPage Members Hash: {viewModel.Members.GetHashCode()}");
        BindingContext = viewModel;

        foreach (var member in viewModel.Members)
        {
            System.Diagnostics.Debug.WriteLine(
                $"PAGE: {member.DisplayName} -> Hosted={member.HostedFlag}, NextHost={member.IsNextHost}");
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        var vm = (GroupMembersViewModel)BindingContext;

        foreach (var member in vm.Members)
        {
            System.Diagnostics.Debug.WriteLine(
                $"APPEARING: {member.DisplayName} -> Hosted={member.HostedFlag}, NextHost={member.IsNextHost}");
        }
    }
}