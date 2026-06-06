namespace BoardGamerApp.Views;

using CommunityToolkit.Maui.Views;

public partial class EventPage : ContentPage
{
    public EventPage()
    {
        InitializeComponent();
    }

    // Öffnet das Popup zum Erstellen eines neuen Events, wenn auf das Plus-Symbol geklickt wird
    private void OnNewEventClicked(object sender, EventArgs e)
    {
        this.ShowPopup(new NewEventPopup());
    }

    private async void OnPreviousEventsClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(PreviousEventsPage));
    }
}