namespace BoardGamerApp.Views;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
		BindingContext = new RatingViewModel();
		BindingContext = new EventViewModel();
	}

	// Navigiert zur Spieleseite wenn auf den nächsten Termin geklickt wird.
	private async void OnGamesClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync(nameof(GamesPage));
	}

	// Navigiert zur Bewertungsseite wenn auf den vergangenen Termin geklickt wird.
	private async void OnRatingClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync(nameof(RatingPage));
	}

	// Navigiert zur Eventseite wenn auf den Termine-Button geklickt wird.
	private async void OnEventClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync(nameof(EventPage));
	}

    private async void OnGroupClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(GroupOverviewPage));
    }
	// Navigiert zur Nachrichten-Seite wenn auf das Nachrichten-Symbol geklickt wird.
	private async void OnMessageClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync(nameof(MessagePage));
	}

}
