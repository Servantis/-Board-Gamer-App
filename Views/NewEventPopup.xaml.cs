using CommunityToolkit.Maui.Views;
using System.ComponentModel;
using BoardGamerApp.Models;
using BoardGamerApp.ViewModels;

namespace BoardGamerApp.Views;

/// <summary>
/// Der Dialog ("Popup", aus dem CommunityToolkit.Maui) zum Anlegen eines neuen Termins.
/// Bekommt beim Öffnen das schon vorhandene <see cref="EventViewModel"/> der EventPage
/// übergeben - es wird also KEIN eigenes ViewModel für das Popup erstellt. Dadurch landet
/// ein neu gespeicherter Termin automatisch in derselben GameNights-Liste, die auch auf
/// der EventPage angezeigt wird.
///
/// Diese Klasse arbeitet bewusst ohne Data Binding (kein eigener BindingContext), sondern
/// greift direkt per x:Name auf die XAML-Elemente zu (LocationPicker, GamePicker, ...).
/// Das ist kein "falsches" MVVM, aber ein einfacherer, code-lastigerer Stil - für ein
/// kleines Popup wie dieses völlig okay.
/// </summary>
public partial class NewEventPopup : Popup
{
    private readonly EventViewModel _viewModel;

    // Datum und Uhrzeit werden über zwei unsichtbare Picker (HiddenDatePicker/
    // HiddenTimePicker, siehe XAML) im Hintergrund erfasst und hier gemerkt, weil es
    // in der UI keine direkte Kombi-Anzeige "Datum + Uhrzeit" gibt.
    private DateTime _selectedDateTime;

    public NewEventPopup(EventViewModel vm)
    {
        InitializeComponent();
        _viewModel = vm;

        _selectedDateTime = DateTime.Now;

        HiddenDatePicker.Date = DateTime.Now;
        HiddenTimePicker.Time = new TimeSpan(12, 0, 0);

        HiddenDatePicker.Unfocused += OnDatePickerClosed_iOS;
        HiddenTimePicker.Unfocused += (s, e) => ApplyDateTime();

        // Auswahllisten kommen aus der DB (werden von EventPage.OnAppearing geladen,
        // BEVOR dieses Popup überhaupt erzeugt wird), damit hier nur echte, vorhandene
        // Orte/Spiele/Spieler ausgewählt werden können - kein Freitext mehr.
        LocationPicker.ItemsSource = _viewModel.Locations;
        GamePicker.ItemsSource = _viewModel.Games;
        HostPicker.ItemsSource = _viewModel.Players;
    }

    // Der Nutzer tippt auf das Datum/Uhrzeit-Feld -> der (normalerweise unsichtbare)
    // Inline-DatePicker wird eingeblendet.
    private void OnDateTimeTapped(object sender, TappedEventArgs e)
    {
        InlineDatePicker.IsVisible = true;
    }

    // Nach der Datumsauswahl direkt den Zeit-Picker anzeigen, damit der Nutzer in
    // einem Zug Datum UND Uhrzeit auswählen kann.
    private void OnInlineDateSelected(object sender, DateChangedEventArgs e)
    {
        InlineTimePicker.IsVisible = true;
    }

    private void OnInlineTimeChanged(object sender, TimeChangedEventArgs e)
    {
        ApplyDateTime();
    }

    // iOS-Besonderheit: dort schließt sich ein Picker nicht automatisch, wenn ein
    // anderes Steuerelement fokussiert wird - deshalb wird hier nach dem Schließen
    // des Datums-Pickers manuell zum Zeit-Picker weitergesprungen.
    private void OnDatePickerClosed_iOS(object sender, FocusEventArgs e)
    {
        if (OperatingSystem.IsIOS())
        {
            HiddenTimePicker.Focus();
        }
    }

    // Führt das gewählte Datum und die gewählte Uhrzeit zu einem DateTime zusammen
    // und aktualisiert das Anzeige-Label.
    private void ApplyDateTime()
    {
        DateTime date = InlineDatePicker.Date ?? DateTime.Now;
        TimeSpan time = InlineTimePicker.Time ?? new TimeSpan(12, 0, 0);

        _selectedDateTime = date + time;

        DateTimeLabel.Text = string.Format("{0:dd.MM.yyyy HH:mm}", _selectedDateTime);
        DateTimeLabel.TextColor = Colors.Black;
    }

    /// <summary>
    /// Wird beim Klick auf "Speichern" ausgeführt. Baut aus den Eingaben ein neues
    /// <see cref="GameNight"/>-Objekt und übergibt es zusammen mit den drei gewählten
    /// Picker-Objekten (Ort/Spiel/Veranstalter) an das ViewModel, das sich um das
    /// eigentliche Speichern in der Datenbank kümmert.
    /// </summary>
    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var newNight = new GameNight
        {
            ScheduledAt = _selectedDateTime.ToUniversalTime().ToString("o"),
            Notes = string.IsNullOrWhiteSpace(NotesEntry.Text) ? null : NotesEntry.Text,
            Status = BoardGamerConstants.GameNightStatus.Planned
        };

        // Picker.SelectedItem ist vom Typ "object" (bzw. object?) - deshalb das
        // "as GameLocation"/"as BoardGame"/"as Player": wurde nichts ausgewählt,
        // ist SelectedItem null und "as ..." liefert dann einfach null zurück
        // (statt eine Exception zu werfen wie ein normaler Cast mit "(GameLocation)").
        //
        // Location/Game/Host sind damit bereits existierende, in der DB gespeicherte
        // Objekte - dadurch können LocationId/HostPlayerId (Foreign Keys) im
        // ViewModel nie ungültige Werte bekommen. GroupId wird von AddGameNightAsync
        // automatisch auf die Standardgruppe gesetzt.
        await _viewModel.AddGameNightAsync(
            newNight,
            LocationPicker.SelectedItem as GameLocation,
            GamePicker.SelectedItem as BoardGame,
            HostPicker.SelectedItem as Player);

        Close();
    }
}
