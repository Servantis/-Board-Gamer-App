namespace BoardGamerApp;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute(nameof(Views.GamesPage), typeof(Views.GamesPage));
		Routing.RegisterRoute(nameof(Views.RatingPage), typeof(Views.RatingPage));
		Routing.RegisterRoute(nameof(Views.EventPage), typeof(Views.EventPage));
		Routing.RegisterRoute(nameof(Views.MessagePage), typeof(Views.MessagePage));
	}
}
