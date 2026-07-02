namespace BoardGamerApp.Views;

using CommunityToolkit.Maui.Views;
using BoardGamerApp.Models;

public partial class EventPage : ContentPage
{
    public EventViewModel ViewModel { get; set; }

    public EventPage()
    {
        InitializeComponent();
        ViewModel = new EventViewModel();
        BindingContext = ViewModel;
    }

    // Öffnet das Popup zum Erstellen eines neuen GameNight-Termins
    private void OnNewEventClicked(object sender, EventArgs e)
    {
        this.ShowPopup(new NewEventPopup(ViewModel));
    }

    // Navigation zur Seite mit vergangenen Events
    private async void OnPreviousEventsClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(PreviousEventsPage),
            new Dictionary<string, object>
            {
                { "GameNights", ViewModel.GameNights }
            });
    }

    private async void OnGamesClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(GamesPage));
    }
}


/*
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

    private async void OnGamesClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(GamesPage));
    }
}
*/