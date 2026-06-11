namespace BoardGamerApp.Views;

using CommunityToolkit.Maui.Views;

public partial class EventPage : ContentPage
{
    public EventViewModel ViewModel { get; set; }
    public EventPage()
    {
        InitializeComponent();
        ViewModel = new EventViewModel();
        BindingContext = ViewModel;
    }

    // Öffnet das Popup zum Erstellen eines neuen Events, wenn auf das Plus-Symbol geklickt wird
    private void OnNewEventClicked(object sender, EventArgs e)
    {
        this.ShowPopup(new NewEventPopup(ViewModel));
    }

    private async void OnPreviousEventsClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(PreviousEventsPage),
            new Dictionary<string, object>
            {
            { "Events", ViewModel.Events }
            });
    }
}