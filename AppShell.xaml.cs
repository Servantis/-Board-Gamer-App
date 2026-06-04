namespace BoardGamerApp;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// Registriert die Navigationsrouten für die einzelnen Seiten.
		// Dadurch kann die App später per Shell-Navigation (GoToAsync) 
		// gezielt zu diesen Views wechseln.

		Routing.RegisterRoute(nameof(Views.GamesPage), typeof(Views.GamesPage));
		Routing.RegisterRoute(nameof(Views.RatingPage), typeof(Views.RatingPage));
		Routing.RegisterRoute(nameof(Views.EventPage), typeof(Views.EventPage));
		Routing.RegisterRoute(nameof(Views.MessagePage), typeof(Views.MessagePage));
	}
}
