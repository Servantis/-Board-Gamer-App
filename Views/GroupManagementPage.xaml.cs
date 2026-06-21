using BoardGamerApp.ViewModels;

namespace BoardGamerApp.Views;

public partial class GroupManagementPage : ContentPage
{
    public GroupManagementPage(GroupMembersViewModel viewModel)
    {
        InitializeComponent();

        BindingContext = viewModel;
    }

    private async void OnEventClicked(object sender, TappedEventArgs e)
    {
        // Anpassen an deine echte Route
        await Shell.Current.GoToAsync("//events");
    }

    private async void OnMessageClicked(object sender, TappedEventArgs e)
    {
        await DisplayAlertAsync("Nachricht", "Nachrichtenfunktion folgt später.", "OK");
    }

    private async void OnGroupClicked(object sender, TappedEventArgs e)
    {
        // Du bist vermutlich schon auf der Gruppenseite.
        await Shell.Current.GoToAsync("//group");
    }
}