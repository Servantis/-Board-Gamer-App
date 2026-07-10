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
/// - <c>new NewEventPopup(vm)</c> -&gt; neuen Termin anlegen (leeres Formular, Gruppe
///   muss im GroupPicker gewählt werden, Gastgeber/Ort ergeben sich automatisch).
/// - <c>new NewEventPopup(vm, night, availableGames, suggestedGames)</c> -&gt; vorhandenen
///   Termin bearbeiten (Formular wird mit den Werten von "night" vorbefüllt, Gruppe/
///   Gastgeber/Ort werden nur noch informativ angezeigt, nicht mehr änderbar).
///
/// Diese Klasse arbeitet bewusst ohne Data Binding (kein eigener BindingContext), sondern
/// greift direkt per x:Name auf die XAML-Elemente zu (GroupPicker, GamesCollectionView, ...).
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
    // statt einen neuen anzulegen (AddGameNightAsync). Bleibt es null, legt das Popup
    // einen neuen Termin an.
    private readonly GameNight? _editingNight;

    /// <summary>
    /// Konstruktor zum Anlegen eines NEUEN Termins (leeres Formular).
    /// </summary>
    public NewEventPopup(EventViewModel vm) : this(vm, null, null, null)
    {
    }

    /// <summary>
    /// Konstruktor zum BEARBEITEN eines bereits vorhandenen Termins. Wird von
    /// EventPage.xaml.cs (OnEditEventClicked) verwendet, wenn auf EventPage ein
    /// Termin angetippt wird.
    ///
    /// <paramref name="editingNight"/> ist der zu bearbeitende Termin selbst (gleiche
    /// Id bleibt beim Speichern erhalten). <paramref name="availableGames"/> sind alle
    /// Spiele der Gruppe dieses Termins (für GamesCollectionView.ItemsSource) und
    /// <paramref name="suggestedGames"/> die aktuell vorgeschlagenen (für die
    /// Vorauswahl) - beide werden schon AUSSERHALB dieses Konstruktors (in
    /// EventPage.xaml.cs, bevor das Popup erzeugt wird) ermittelt, weil das asynchron
    /// (Datenbankzugriff) passiert - ein Konstruktor kann aber kein "await" benutzen.
    /// </summary>
    public NewEventPopup(
        EventViewModel vm,
        GameNight? editingNight,
        IReadOnlyList<BoardGame>? availableGames,
        IReadOnlyList<BoardGame>? suggestedGames)
    {
        InitializeComponent();
        _viewModel = vm;
        _editingNight = editingNight;

        // Gruppen kommen aus der DB (werden von EventPage.OnAppearing geladen, BEVOR
        // dieses Popup überhaupt erzeugt wird) - schon gefiltert auf die Gruppen, denen
        // der aktuelle Spieler angehört (siehe EventViewModel.LoadReferenceDataAsync).
        GroupPicker.ItemsSource = _viewModel.Groups;

        if (editingNight is not null)
        {
            // --- Bearbeiten-Modus: vorhandene Werte des Termins vorbelegen ---
            TitleLabel.Text = "Termin bearbeiten";

            // Gruppe/Ort/Gastgeber wurden beim Anlegen fest zugeordnet und lassen sich
            // nachträglich nicht mehr ändern - deshalb hier nur noch informativ als Text,
            // statt als auswählbarer Picker.
            GroupSelectionSection.IsVisible = false;
            ReadOnlyInfoSection.IsVisible = true;

            GroupInfoLabel.Text = $"Gruppe: {editingNight.GroupName}";
            LocationInfoLabel.Text = $"Ort: {editingNight.LocationName}";
            HostInfoLabel.Text = $"Gastgeber: {editingNight.HostName}";

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

            GamesCollectionView.ItemsSource = availableGames ?? new List<BoardGame>();

            // Wichtig: CollectionView.SelectedItems muss genau dieselben Objekte
            // (Referenz) enthalten, die auch in der ItemsSource-Liste stecken - deshalb
            // reicht EventPage.xaml.cs hier dieselben BoardGame-Instanzen herein, statt
            // dass hier neue Objekte gebaut werden.
            if (suggestedGames is not null && suggestedGames.Count > 0)
            {
                GamesCollectionView.SelectedItems = suggestedGames.Cast<object>().ToList();
            }

            NotesEntry.Text = editingNight.Notes;
        }
        else
        {
            // --- Neu-Anlegen-Modus: Datum/Uhrzeit mit dem aktuellen Zeitpunkt vorbelegen ---
            _selectedDateTime = DateTime.Now;

            HiddenDatePicker.Date = DateTime.Now;
            HiddenTimePicker.Time = new TimeSpan(12, 0, 0);

            ReadOnlyInfoSection.IsVisible = false;
            GroupSelectionSection.IsVisible = true;

            AutoAssignInfoLabel.Text = "Bitte zuerst eine Gruppe wählen.";
            AutoAssignInfoLabel.TextColor = Colors.Gray;
        }

        // GamesFieldLabel zeigt direkt beim Öffnen des Popups die schon vorbelegten
        // Spiele an (oder den Platzhaltertext, falls noch keine ausgewählt sind).
        UpdateGamesFieldLabel();

        HiddenDatePicker.Unfocused += OnDatePickerClosed_iOS;
        HiddenTimePicker.Unfocused += (s, e) => ApplyDateTime();
    }

    // Wird ausgelöst, sobald im Anlegen-Modus eine Gruppe im GroupPicker gewählt wird.
    // Ermittelt daraus automatisch den Ort (eigener Ort des aktuellen Spielers in dieser
    // Gruppe, siehe EventViewModel.GetOwnedLocationAsync) und lädt die zu dieser Gruppe
    // gehörenden Spiele neu in GamesCollectionView - hat der aktuelle Spieler keinen
    // eigenen Ort in der Gruppe, wird das über AutoAssignInfoLabel als Fehler angezeigt
    // (die eigentliche Blockade passiert beim Speichern in EventViewModel.AddGameNightAsync).
    private async void OnGroupPickerChanged(object sender, EventArgs e)
    {
        var selectedGroup = GroupPicker.SelectedItem as GamingGroup;

        if (selectedGroup is null)
        {
            AutoAssignInfoLabel.Text = "Bitte zuerst eine Gruppe wählen.";
            AutoAssignInfoLabel.TextColor = Colors.Gray;

            GamesCollectionView.ItemsSource = null;
            UpdateGamesFieldLabel();

            return;
        }

        var ownedLocation = await _viewModel.GetOwnedLocationAsync(selectedGroup.Id);

        if (ownedLocation is null)
        {
            AutoAssignInfoLabel.Text =
                $"Du hast in \"{selectedGroup.Name}\" noch keinen eigenen Ort hinterlegt. " +
                "Ein Termin kann hier erst angelegt werden, sobald ein eigener Ort existiert.";
            AutoAssignInfoLabel.TextColor = Colors.Firebrick;
        }
        else
        {
            AutoAssignInfoLabel.Text = $"Gastgeber: {_viewModel.CurrentPlayerName} - Ort: {ownedLocation.Name}";
            AutoAssignInfoLabel.TextColor = Colors.Gray;
        }

        var games = await _viewModel.GetGamesForGroupAsync(selectedGroup.Id);
        GamesCollectionView.ItemsSource = games;

        UpdateGamesFieldLabel();
    }

    // Tippt der Nutzer auf das Spiele-Feld, klappt die Checkbox-Liste darunter auf
    // bzw. wieder zu - optisch angelehnt an das Öffnen eines nativen Pickers wie bei
    // Gruppe/Datum, nur eben mit einer Mehrfachauswahl-Liste statt einer nativen
    // Einzelauswahl.
    private void OnGamesFieldTapped(object sender, TappedEventArgs e)
    {
        GamesCollectionView.IsVisible = !GamesCollectionView.IsVisible;
    }

    // Wird bei jeder Änderung der Auswahl in GamesCollectionView ausgelöst (Antippen
    // einer Zeile) und aktualisiert die Zusammenfassung im Spiele-Feld.
    private void OnGamesSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateGamesFieldLabel();
    }

    // Zeigt im Spiele-Feld die Titel aller aktuell ausgewählten Spiele an (durch Komma
    // getrennt), oder den Platzhaltertext "Spiele vorschlagen", solange noch nichts
    // ausgewählt wurde.
    private void UpdateGamesFieldLabel()
    {
        var titles = GamesCollectionView.SelectedItems
            .OfType<BoardGame>()
            .Select(g => g.Title)
            .ToList();

        if (titles.Count > 0)
        {
            GamesFieldLabel.Text = string.Join(", ", titles);
            GamesFieldLabel.TextColor = Colors.Black;
        }
        else
        {
            GamesFieldLabel.Text = "Spiele vorschlagen";
            GamesFieldLabel.TextColor = Colors.Gray;
        }
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
    ///   und übergibt es zusammen mit der gewählten Gruppe an EventViewModel.AddGameNightAsync.
    /// - Bearbeiten: übernimmt die neuen Eingaben in den vorhandenen _editingNight
    ///   (gleiche Id!) und übergibt ihn an EventViewModel.UpdateGameNightAsync.
    ///
    /// AddGameNightAsync/UpdateGameNightAsync geben ein bool zurück (Erfolg oder nicht) -
    /// das Popup schließt sich nur bei Erfolg. So bleibt z. B. bei einer fehlenden
    /// Gruppenauswahl oder einem fehlenden eigenen Ort das Popup offen, und der Nutzer
    /// kann die Eingabe korrigieren, statt dass der Termin stillschweigend verworfen wird.
    /// </summary>
    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var selectedGames = GamesCollectionView.SelectedItems
            .OfType<BoardGame>()
            .ToList();

        bool success;

        if (_editingNight is not null)
        {
            // Bearbeiten-Modus: den vorhandenen Termin (gleiche Id) mit den neuen
            // Eingaben aktualisieren. Gruppe/Gastgeber/Ort bleiben unverändert.
            _editingNight.ScheduledAt = _selectedDateTime.ToUniversalTime().ToString("o");
            _editingNight.Notes = string.IsNullOrWhiteSpace(NotesEntry.Text) ? null : NotesEntry.Text;

            success = await _viewModel.UpdateGameNightAsync(_editingNight, selectedGames);
        }
        else
        {
            var selectedGroup = GroupPicker.SelectedItem as GamingGroup;

            if (selectedGroup is null)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Gruppe fehlt",
                    "Bitte wähle zuerst eine Gruppe für diesen Termin aus.",
                    "OK");

                return;
            }

            // Neu-Anlegen-Modus: GroupId/HostPlayerId/LocationId setzt AddGameNightAsync
            // selbst (Gastgeber = aktueller Spieler, Ort = dessen eigener Ort in der Gruppe).
            var newNight = new GameNight
            {
                ScheduledAt = _selectedDateTime.ToUniversalTime().ToString("o"),
                Notes = string.IsNullOrWhiteSpace(NotesEntry.Text) ? null : NotesEntry.Text,
                Status = BoardGamerConstants.GameNightStatus.Planned
            };

            success = await _viewModel.AddGameNightAsync(newNight, selectedGroup, selectedGames);
        }

        if (success)
            Close();
    }
}
