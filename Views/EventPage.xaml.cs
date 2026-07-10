namespace BoardGamerApp.Views;

using CommunityToolkit.Maui.Views;
using BoardGamerApp.Models;
using BoardGamerApp.ViewModels;

/// <summary>
/// Code-Behind zur Hauptseite der Terminverwaltung (EventPage.xaml).
/// Diese Klasse enthält bewusst wenig Logik - die steckt im <see cref="EventViewModel"/>.
/// Hier passiert nur "UI-Klebstoff": Popup öffnen, Navigation auslösen, Daten beim
/// Erscheinen der Seite nachladen.
/// </summary>
public partial class EventPage : ContentPage
{
    // Das ViewModel wird der Page per Dependency Injection übergeben (siehe Konstruktor
    // und die Registrierung "builder.Services.AddTransient&lt;EventViewModel&gt;()" in
    // MauiProgram.cs). "BindingContext = ViewModel" verbindet dann die XAML-Bindings
    // ({Binding UpcomingGameNights} usw.) mit genau diesem ViewModel-Objekt.
    public EventViewModel ViewModel { get; set; }

    public EventPage(EventViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        BindingContext = ViewModel;
    }

    /// <summary>
    /// Wird von .NET MAUI automatisch aufgerufen, JEDES Mal wenn die Seite sichtbar wird
    /// (also auch beim Zurücknavigieren von einer anderen Seite). Deshalb ist das der
    /// richtige Ort, um die aktuellen Daten aus der Datenbank zu laden - so sind die
    /// Termine immer up-to-date, auch wenn sich zwischenzeitlich etwas geändert hat.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Erst die eigentlichen Termine laden, ...
        await ViewModel.LoadGameNightsAsync();

        // ... dann die Gruppen (Groups), denen der aktuelle Spieler angehört, für den
        // Gruppen-Picker im "Neuer Termin"-Popup. Das passiert schon hier und nicht erst
        // beim Öffnen des Popups selbst, damit der Picker sofort befüllt ist, sobald der
        // Nutzer auf "+" tippt.
        await ViewModel.LoadReferenceDataAsync();
    }

    // Öffnet das Popup zum Erstellen eines neuen GameNight-Termins
    private void OnNewEventClicked(object sender, EventArgs e)
    {
        this.ShowPopup(new NewEventPopup(ViewModel));
    }

    // Navigation zur Seite mit vergangenen Events. Die komplette Liste der Termine
    // wird als Navigations-Parameter mitgegeben (siehe [QueryProperty] in
    // PreviousEventsPage.xaml.cs), dort wird dann nur noch nach "in der Vergangenheit"
    // gefiltert.
    private async void OnPreviousEventsClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(PreviousEventsPage),
            new Dictionary<string, object>
            {
                { "GameNights", ViewModel.GameNights }
            });
    }

    /// <summary>
    /// Wird beim Antippen eines Termins in der Liste (CollectionView.ItemTemplate)
    /// ausgelöst - siehe TapGestureRecognizer in EventPage.xaml. Öffnet das
    /// NewEventPopup im Bearbeiten-Modus für GENAU diesen Termin - aber nur, wenn der
    /// aktuelle Spieler das auch darf (siehe GameNight.CanBeEditedByCurrentPlayer: nur
    /// der Gastgeber, und nur solange der Termin nicht abgesagt ist). Andere
    /// Gruppenmitglieder bekommen stattdessen einen Hinweis - sie können höchstens über
    /// die Zusagen/Absagen-Buttons auf der Karte ihre eigene Antwort ändern.
    ///
    /// Woher kommt der angetippte Termin? "sender" ist hier der TapGestureRecognizer
    /// selbst (der erbt in .NET MAUI von Element). Ein TapGestureRecognizer, der
    /// innerhalb eines DataTemplates deklariert wird, bekommt automatisch denselben
    /// BindingContext wie sein umgebendes Element - in unserem Fall also genau den
    /// GameNight, für den diese Karte gerade angezeigt wird.
    /// </summary>
    private async void OnEditEventClicked(object? sender, EventArgs e)
    {
        if (sender is not Element element || element.BindingContext is not GameNight night)
            return;

        if (!night.CanBeEditedByCurrentPlayer)
        {
            var message = night.IsCancelled
                ? "Dieser Termin wurde abgesagt und kann nicht mehr bearbeitet werden."
                : "Nur der Gastgeber kann diesen Termin bearbeiten. Du kannst hier höchstens deine Zusage oder Absage ändern.";

            await Shell.Current.DisplayAlertAsync("Bearbeiten nicht möglich", message, "OK");
            return;
        }

        // Welche Spiele aktuell zu diesem Termin vorgeschlagen sind, und welche Spiele in
        // der Gruppe dieses Termins überhaupt zur Auswahl stehen, steckt nicht direkt in
        // "night" selbst - deshalb erst hier laden, BEVOR das Popup erzeugt wird (ein
        // Konstruktor kann nicht "await" benutzen).
        var availableGames = await ViewModel.GetGamesForGroupAsync(night.GroupId);
        var suggestedGames = await ViewModel.GetSuggestedGamesAsync(night);

        this.ShowPopup(new NewEventPopup(ViewModel, night, availableGames, suggestedGames));
    }

    // Zusagen/Absagen direkt aus der Terminliste: "sender" ist der jeweilige Button,
    // dessen BindingContext (geerbt von der Karte, siehe EventPage.xaml) genau der
    // GameNight ist, für den diese Karte gerade angezeigt wird. Nur sichtbar/aktiv, wenn
    // GameNight.CanRespondToAttendance true ist (nicht Gastgeber, Termin noch "planned").
    private async void OnAcceptClicked(object? sender, EventArgs e)
    {
        if (sender is not Element element || element.BindingContext is not GameNight night)
            return;

        await ViewModel.RespondToAttendanceAsync(night, BoardGamerConstants.AttendanceStatus.Accepted);
    }

    private async void OnDeclineClicked(object? sender, EventArgs e)
    {
        if (sender is not Element element || element.BindingContext is not GameNight night)
            return;

        await ViewModel.RespondToAttendanceAsync(night, BoardGamerConstants.AttendanceStatus.Declined);
    }
}
