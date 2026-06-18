using BoardGamerApp.Views;
using System.Diagnostics;

namespace BoardGamerApp;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

        // Registriert die Navigationsrouten für die einzelnen Seiten.
        // Dadurch kann die App später per Shell-Navigation (GoToAsync) 
        // gezielt zu diesen Views wechseln.

        //Game Library
        Routing.RegisterRoute(nameof(Views.GameLibrary), typeof(Views.GameLibrary));
        Routing.RegisterRoute(nameof(AddGameView), typeof(AddGameView));

        Routing.RegisterRoute(nameof(Views.GamesPage), typeof(Views.GamesPage));
        Routing.RegisterRoute(nameof(Views.RatingPage), typeof(Views.RatingPage));
		Routing.RegisterRoute(nameof(Views.EventPage), typeof(Views.EventPage));
        Routing.RegisterRoute(nameof(Views.GroupPage), typeof(Views.GroupPage));
        Routing.RegisterRoute(nameof(Views.GroupManagementPage), typeof(Views.GroupManagementPage));
        Routing.RegisterRoute(nameof(Views.MessagePage), typeof(Views.MessagePage));
		Routing.RegisterRoute(nameof(Views.PreviousEventsPage), typeof(Views.PreviousEventsPage));


#if DEBUG
        Routing.RegisterRoute(nameof(SyncOutboxDebugView), typeof(SyncOutboxDebugView));
        DebugFlyoutItem.IsVisible = Debugger.IsAttached;
#else
        DebugFlyoutItem.IsVisible = false;

#endif


    }
}
