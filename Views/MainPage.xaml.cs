namespace BoardGamerApp.Views;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
		BindingContext = new RatingViewModel();
	}

	private async void OnGamesClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync(nameof(GamesPage));
	}

	private async void OnRatingClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync(nameof(RatingPage));
	}

	private async void OnEventClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync(nameof(EventPage));
	}

	private async void OnMessageClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync(nameof(MessagePage));
	}

}
