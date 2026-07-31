using BoardGamerApp.Services;
using BoardGamerApp.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace BoardGamerApp;

public partial class AppShell : Shell
{
    private readonly CurrentPlayerService _currentPlayerService;
    private readonly IServiceProvider _serviceProvider;

    private ToolbarItem? _playerToolbarItem;

    public AppShell(
        CurrentPlayerService currentPlayerService,
        IServiceProvider serviceProvider)
    {
        InitializeComponent();

        _currentPlayerService = currentPlayerService;
        _serviceProvider = serviceProvider;

        _currentPlayerService.CurrentPlayerChanged += OnCurrentPlayerChanged;

        RegisterRoutes();

        UpdatePlayerToolbarItem();

        // Das Debug-FlyoutItem wird bewusst NICHT hier direkt (synchron, sofort nach
        // InitializeComponent) sichtbar/unsichtbar geschaltet: Auf Android hat Shell ein
        // bekanntes Problem damit, wenn die IsVisible-Eigenschaft eines FlyoutItems
        // umgeschaltet wird, BEVOR das native Flyout-Menü (intern über RecyclerView/
        // ViewPager2 gerendert) seinen ersten Layout-Durchlauf komplett abgeschlossen hat -
        // das führt zu einem inkonsistenten Adapter-Zustand und kann später beim Antippen
        // eines Menüpunkts zu einem Absturz/Einfrieren führen (RecyclerView/ViewPager2/
        // AppBarLayout/DrawerLayout tief im Android-Stacktrace). Deshalb wird die Änderung
        // hier auf "nach dem ersten Laden der Shell" verschoben (Loaded-Event + Dispatcher),
        // damit das native Flyout-Menü zu diesem Zeitpunkt schon vollständig fertig
        // aufgebaut ist.
        Loaded += OnShellLoaded;
    }

    private void OnShellLoaded(object? sender, EventArgs e)
    {
        Loaded -= OnShellLoaded;

        Dispatcher.Dispatch(() =>
        {
#if DEBUG
            DebugFlyoutItem.IsVisible = Debugger.IsAttached;
#else
            DebugFlyoutItem.IsVisible = false;
#endif
        });
    }

    private void RegisterRoutes()
    {
        // Game Library
        Routing.RegisterRoute(nameof(GameLibrary), typeof(GameLibrary));
        Routing.RegisterRoute(nameof(AddGameView), typeof(AddGameView));

        // Hauptseiten
        Routing.RegisterRoute(nameof(GamesPage), typeof(GamesPage));
        Routing.RegisterRoute(nameof(RatingPage), typeof(RatingPage));
        Routing.RegisterRoute(nameof(EventPage), typeof(EventPage));
        Routing.RegisterRoute(nameof(PreviousEventsPage), typeof(PreviousEventsPage));
        Routing.RegisterRoute(nameof(GroupOverviewPage), typeof(GroupOverviewPage));

        // Gruppen Seiten 
        Routing.RegisterRoute(nameof(AddGroupPage), typeof(AddGroupPage));
        Routing.RegisterRoute(nameof(AddPlayerPage), typeof(AddPlayerPage));
        Routing.RegisterRoute(nameof(GroupPage), typeof(GroupPage));
        Routing.RegisterRoute(nameof(GroupManagementPage), typeof(GroupManagementPage));
        Routing.RegisterRoute(nameof(PreviousEventsPage), typeof(PreviousEventsPage));
        Routing.RegisterRoute(nameof(GameNightSuggestionsPage),typeof(GameNightSuggestionsPage));

        // Spielerprofil
        Routing.RegisterRoute(nameof(PlayerProfilePage), typeof(PlayerProfilePage));

        // Eine Shell-Route ist dafür nicht zwingend nötig.
        // Falls du später per GoToAsync navigieren willst, kannst du sie trotzdem registrieren:
        // Routing.RegisterRoute(nameof(PlayerProfilePage), typeof(PlayerProfilePage));

#if DEBUG
        Routing.RegisterRoute(nameof(SyncOutboxDebugView), typeof(SyncOutboxDebugView));
        Routing.RegisterRoute(nameof(PlayerSelectionPage), typeof(PlayerSelectionPage));
        Routing.RegisterRoute(nameof(LoadingPage), typeof(LoadingPage));
#endif
    }

    private void OnCurrentPlayerChanged()
    {
        MainThread.BeginInvokeOnMainThread(UpdatePlayerToolbarItem);
    }

    private void UpdatePlayerToolbarItem()
    {
        if (_currentPlayerService.IsLoggedIn)
        {
            if (_playerToolbarItem is null)
            {
                _playerToolbarItem = new ToolbarItem
                {
                    Text = _currentPlayerService.PlayerName ?? "Spieler",
                    IconImageSource = "player_default.png",
                    Order = ToolbarItemOrder.Primary,
                    Priority = 0
                };

                _playerToolbarItem.Clicked += OnPlayerToolbarItemClicked;

                ToolbarItems.Add(_playerToolbarItem);
            }

            _playerToolbarItem.Text = _currentPlayerService.PlayerName ?? "Spieler";
            return;
        }

        if (_playerToolbarItem is not null)
        {
            _playerToolbarItem.Clicked -= OnPlayerToolbarItemClicked;
            ToolbarItems.Remove(_playerToolbarItem);
            _playerToolbarItem = null;
        }
    }

    private async void OnPlayerToolbarItemClicked(object? sender, EventArgs e)
    {
        if (!_currentPlayerService.IsLoggedIn)
        {
            return;
        }

        var profilePage = _serviceProvider.GetRequiredService<PlayerProfilePage>();

        await Navigation.PushModalAsync(profilePage, true);
    }
}