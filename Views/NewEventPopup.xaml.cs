using CommunityToolkit.Maui.Views;
using System.ComponentModel;
using System.Linq;
using BoardGamerApp.Models;
using BoardGamerApp.ViewModels;

namespace BoardGamerApp.Views;

/// <summary>
/// Der Dialog ("Popup", aus dem CommunityToolkit.Maui) zum Anlegen ODER Bearbeiten
/// eines Termins. Bekommt beim Öffnen das schon vorhandene <see cref="EventViewModel"/>
/// der EventPage übergeben - es wird also KEIN eigenes ViewModel für das Popup erstellt.
/// Dadurch landet ein neu gespeicherter/aktualisierter Termin automatisch in derselben
/// GameNights-Liste, die auch auf der EventPage angezeigt wird.
///
/// Zwei Konstruktoren, zwei Modi (siehe auch <see cref="_editingNight"/>):
/// - <c>new NewEventPopup(vm)</c> -&gt; neuen Termin anlegen (leeres Formular).
/// - <c>new NewEventPopup(vm, night, suggestedGame)</c> -&gt; vorhandenen Termin bearbeiten
///   (Formular wird mit den Werten von "night" vorbefüllt).
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

    // Ist dieses Feld gesetzt, befindet sich das Popup im "Bearbeiten"-Modus:
    // OnSaveClicked aktualisiert dann diesen vorhandenen Termin (UpdateGameNightAsync),
    // statt einen neuen anzulegen (AddGameNightAsync). Bleibt es null, verhält sich
    // das Popup genau wie bisher (neuer Termin).
    private readonly GameNight? _editingNight;

    /// <summary>
    /// Konstruktor zum Anlegen eines NEUEN Termins (bisheriges Verhalten, unverändert).
    /// </summary>
    public NewEventPopup(EventViewModel vm) : this(vm, null, null)
    {
    }

    /// <summary>
    /// Konstruktor zum BEARBEITEN eines bereits vorhandenen Termins. Wird von
    /// EventPage.xaml.cs (OnEditEventClicked) verwendet, wenn auf EventPage ein
    /// Termin angetippt wird.
    ///
    /// <paramref name="editingNight"/> ist der zu bearbeitende Termin selbst (gleiche
    /// Id bleibt beim Speichern erhalten). <paramref name="suggestedGame"/> muss
    /// VORHER (also außerhalb dieses Konstruktors) per
    /// <see cref="EventViewModel.GetSuggestedGameAsync"/> ermittelt werden, weil das
    /// Ermitteln asynchron ist (Datenbankzugriff) - ein Konstruktor kann aber nicht
    /// "await" verwenden. Deshalb übernimmt EventPage.xaml.cs diesen Schritt, bevor
    /// das Popup überhaupt erzeugt wird.
    /// </summary>
    public NewEventPopup(EventViewModel vm, GameNight? editingNight, BoardGame? suggestedGame)
    {
        InitializeComponent();
        _viewModel = vm;
        _editingNight = editingNight;

        // Auswahllisten kommen aus der DB (werden von EventPage.OnAppearing geladen,
        // BEVOR dieses Popup überhaupt erzeugt wird), damit hier nur echte, vorhandene
        // Orte/Spiele/Spieler ausgewählt werden können - kein Freitext mehr.
        LocationPicker.ItemsSource = _viewModel.Locations;
        GamePicker.ItemsSource = _viewModel.Games;
        HostPicker.ItemsSource = _viewModel.Players;

        if (editingNight is not null)
        {
            // --- Bearbeiten-Modus: vorhandene Werte des Termins vorbelegen ---
            TitleLabel.Text = "Termin bearbeiten";

            // ScheduledAt ist in der DB als UTC-ISO-String gespeichert (siehe GameNight.cs) -
            // für die Anzeige/Bearbeitung wandeln wir es wieder in lokale Zeit um.
            _selectedDateTime = DateTime.Parse(
                editingNight.ScheduledAt,
                null,
                System.Globalization.DateTimeStyles.RoundtripKind).ToLocalTime();

            HiddenDatePicker.Date = _selectedDateTime.Date;
            HiddenTimePicker.Time = _selectedDateTime.TimeOfDay;

            DateTimeLabel.Text = string.Format("{0:dd.MM.yyyy HH:mm}", _selectedDateTime);
            DateTimeLabel.TextColor = Colors.Black;

            // Wichtig: Picker.SelectedItem muss genau dasselbe Objekt (Referenz) sein,
            // das auch in der ItemsSource-Liste steckt - deshalb hier NICHT einfach ein
            // neues Objekt mit passender Id bauen, sondern in Locations/Players nach der
            // schon vorhandenen Instanz suchen (FirstOrDefault mit Id-Vergleich).
            LocationPicker.SelectedItem = _viewModel.Locations
                .FirstOrDefault(l => l.Id == editingNight.LocationId);

            HostPicker.SelectedItem = _viewModel.Players
                .FirstOrDefault(p => p.Id == editingNight.HostPlayerId);

            // Das aktuell vorgeschlagene Spiel wurde bereits vom Aufrufer (EventPage.xaml.cs)
            // über EventViewModel.GetSuggestedGameAsync ermittelt und hier hereingereicht.
            GamePicker.SelectedItem = suggestedGame;

            NotesEntry.Text = editingNight.Notes;
        }
        else
        {
            // --- Neu-Anlegen-Modus: wie bisher mit "jetzt" vorbelegen ---
            _selectedDateTime = DateTime.Now;

            HiddenDatePicker.Date = DateTime.Now;
            HiddenTimePicker.Time = new TimeSpan(12, 0, 0);
        }

        HiddenDatePicker.Unfocused += OnDatePickerClosed_iOS;
        HiddenTimePicker.Unfocused += (s, e) => ApplyDateTime();
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
    /// Wird beim Klick auf "Speichern" ausgeführt.
    ///
    /// Zwei Fälle, je nachdem ob das Popup im Bearbeiten-Modus geöffnet wurde
    /// (_editingNight != null) oder nicht:
    /// - Neu anlegen: baut aus den Eingaben ein neues <see cref="GameNight"/>-Objekt
    ///   und übergibt es an EventViewModel.AddGameNightAsync (wie bisher).
    /// - Bearbeiten: übernimmt die neuen Eingaben in den vorhandenen _editingNight
    ///   (gleiche Id!) und übergibt ihn an EventViewModel.UpdateGameNightAsync.
    ///
    /// In beiden Fällen gilt: Location/Game/Host sind bereits existierende, in der DB
    /// gespeicherte Objekte (aus den Pickern) - dadurch können LocationId/HostPlayerId
    /// (Foreign Keys) nie ungültige Werte bekommen.
    /// </summary>
    private async void OnSaveClicked(object sender, EventArgs e)
    {
        // Picker.SelectedItem ist vom Typ "object" (bzw. object?) - deshalb das
        // "as GameLocation"/"as BoardGame"/"as Player": wurde nichts ausgewählt,
        // ist SelectedItem null und "as ..." liefert dann einfach null zurück
        // (statt eine Exception zu werfen wie ein normaler Cast mit "(GameLocation)").
        var selectedLocation = LocationPicker.SelectedItem as GameLocation;
        var selectedGame = GamePicker.SelectedItem as BoardGame;
        var selectedHost = HostPicker.SelectedItem as Player;

        if (_editingNight is not null)
        {
            // Bearbeiten-Modus: den vorhandenen Termin (gleiche Id) mit den neuen
            // Eingaben aktualisieren.
            _editingNight.ScheduledAt = _selectedDateTime.ToUniversalTime().ToString("o");
            _editingNight.Notes = string.IsNullOrWhiteSpace(NotesEntry.Text) ? null : NotesEntry.Text;

            await _viewModel.UpdateGameNightAsync(
                _editingNight,
                selectedLocation,
                selectedGame,
                selectedHost);
        }
        else
        {
            // Neu-Anlegen-Modus (bisheriges Verhalten): GroupId wird von
            // AddGameNightAsync automatisch auf die Standardgruppe gesetzt.
            var newNight = new GameNight
            {
                ScheduledAt = _selectedDateTime.ToUniversalTime().ToString("o"),
                Notes = string.IsNullOrWhiteSpace(NotesEntry.Text) ? null : NotesEntry.Text,
                Status = BoardGamerConstants.GameNightStatus.Planned
            };

            await _viewModel.AddGameNightAsync(
                newNight,
                selectedLocation,
                selectedGame,
                selectedHost);
        }

        Close();
    }
}
