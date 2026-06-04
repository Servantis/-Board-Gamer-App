namespace BoardGamerApp.Views;

using CommunityToolkit.Maui.Views;

public partial class EventPage : ContentPage
{
    public EventPage()
    {
        InitializeComponent();
    }

    private void OnEventClicked(object sender, EventArgs e)
    {
        // Navigation zur EventPage oder anderer Logik
        Shell.Current.GoToAsync(nameof(EventPage));
    }

    private void OnMessageClicked(object sender, EventArgs e)
    {
        // Hier öffnest du später dein Popup für "Neuer Termin"
        Console.WriteLine("Plus-Button geklickt");
    }

    private void OnNewEventClicked(object sender, EventArgs e)
    {
        this.ShowPopup(new NewEventPopup());
    }
}