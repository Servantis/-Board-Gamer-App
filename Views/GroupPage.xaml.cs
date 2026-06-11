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

    // Navigiert zur Eventseite wenn auf den Termine-Button geklickt wird.
    private async void OnEventClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(EventPage));
    }

    // Navigiert zur Groupseite wenn auf den Gruppen-Button geklickt wird.
    private async void OnGroupClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(GroupPage));
    }

    private async void OnMessageClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(MessagePage));
    }
}