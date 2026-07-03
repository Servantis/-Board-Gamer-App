namespace BoardGamerApp.Views;

using BoardGamerApp.ViewModels;

public partial class MainPage : ContentPage
{
	public EventViewModel ViewModel { get; }

	public MainPage(EventViewModel viewModel)
	{
		InitializeComponent();
		ViewModel = viewModel;
		BindingContext = ViewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		await ViewModel.LoadGameNightsAsync();
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
        await Shell.Current.GoToAsync(nameof(GroupPage));
    }
	// Navigiert zur Nachrichten-Seite wenn auf das Nachrichten-Symbol geklickt wird.
	private async void OnMessageClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync(nameof(MessagePage));
	}

}
