using CommunityToolkit.Maui.Views;
using System.ComponentModel;

namespace BoardGamerApp.Views;

public partial class NewEventPopup : Popup
{
    private readonly EventViewModel _viewModel;

    private DateTime _selectedDateTime;

    public NewEventPopup(EventViewModel vm)
    {
        InitializeComponent();
        _viewModel = vm;

        _selectedDateTime = DateTime.Now;

        HiddenDatePicker.Date = DateTime.Now;
        HiddenTimePicker.Time = new TimeSpan(12, 0, 0);

        HiddenDatePicker.DateSelected += OnDateSelected;
        HiddenDatePicker.Unfocused += OnDatePickerClosed_iOS;

        HiddenTimePicker.PropertyChanged += OnTimeChanged;
    }

    private void OnDateTimeTapped(object sender, TappedEventArgs e)
    {
        HiddenDatePicker.Focus();
    }

    private void OnDateSelected(object sender, DateChangedEventArgs e)
    {
        // Android: sofort wechseln
        if (OperatingSystem.IsAndroid())
        {
            HiddenTimePicker.Focus();
        }
    }

    private void OnDatePickerClosed_iOS(object sender, FocusEventArgs e)
    {
        // iOS: erst wechseln, wenn der Nutzer den Picker schließt
        if (OperatingSystem.IsIOS())
        {
            HiddenTimePicker.Focus();
        }
    }

    private void OnTimeChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TimePicker.Time))
        {
            ApplyDateTime();
        }
    }

    private void ApplyDateTime()
    {
        DateTime date = HiddenDatePicker.Date ?? DateTime.Now;
        TimeSpan time = HiddenTimePicker.Time ?? new TimeSpan(12, 0, 0);

        _selectedDateTime = date + time;

        DateTimeLabel.Text = _selectedDateTime.ToString("dd.MM.yyyy HH:mm");
        DateTimeLabel.TextColor = Colors.Black;
    }

    private void OnSaveClicked(object sender, EventArgs e)
    {
        var newEvent = new BoardGameEvent
        {
            Date = _selectedDateTime,
            Location = LocationEntry.Text,
            Game = GameEntry.Text,
            Host = HostEntry.Text
        };

        _viewModel.AddEvent(newEvent);
        Close();
    }
}


/*
using CommunityToolkit.Maui.Views;

namespace BoardGamerApp.Views;

public partial class NewEventPopup : Popup
{
    // Das ViewModel, in das der neue Termin eingetragen wird
    private readonly EventViewModel _viewModel;

    // Speichert das endgültig ausgewählte Datum + Uhrzeit
    private DateTime _selectedDateTime;

    public NewEventPopup(EventViewModel vm)
    {
        InitializeComponent();
        _viewModel = vm;

        // Standardwerte setzen (falls der Nutzer nichts auswählt)
        _selectedDateTime = DateTime.Now;
        HiddenDatePicker.Date = DateTime.Now;
        HiddenTimePicker.Time = new TimeSpan(12, 0, 0);

        // Events nur EINMAL registrieren
        // Wird ausgelöst, wenn der Nutzer den DatePicker bzw. TimePicker schließt
        HiddenDatePicker.Unfocused += OnDatePickerClosed;
        HiddenTimePicker.Unfocused += OnTimePickerClosed;
    }

    // Wird ausgeführt, wenn der Nutzer auf das sichtbare Feld tippt, um Datum/Uhrzeit auszuwählen
    private void OnDateTimeTapped(object sender, TappedEventArgs e)
    {
        // Öffnet den unsichtbaren DatePicker, damit der Nutzer ein Datum auswählen kann
        HiddenDatePicker.Focus();
    }

    // Wird ausgelöst, wenn der Nutzer den DatePicker schließt
    private void OnDatePickerClosed(object sender, FocusEventArgs e)
    {
        // Danach direkt den TimePicker öffnen
        HiddenTimePicker.Focus();
    }

    // Wird ausgelöst, wenn der Nutzer den TimePicker schließt
    private void OnTimePickerClosed(object sender, FocusEventArgs e)
    {
        // MAUI liefert Date und Time als nullable → daher ?? als Fallback
        DateTime date = HiddenDatePicker.Date ?? DateTime.Now;
        TimeSpan time = HiddenTimePicker.Time ?? new TimeSpan(12, 0, 0);

        // Datum + Uhrzeit kombinieren
        _selectedDateTime = date + time;

        // Anzeige im UI aktualisieren
        DateTimeLabel.Text = _selectedDateTime.ToString("dd.MM.yyyy HH:mm");
        DateTimeLabel.TextColor = Colors.Black;
    }

    // Wird ausgeführt, wenn der Nutzer auf "Speichern" klickt
    private void OnSaveClicked(object sender, EventArgs e)
    {
        // Neues Event-Objekt erstellen und mit den eingegebenen Daten füllen
        var newEvent = new BoardGameEvent
        {
            Date = _selectedDateTime,
            Location = LocationEntry.Text,
            Game = GameEntry.Text,
            Host = HostEntry.Text
        };

        // Event ins ViewModel eintragen
        _viewModel.AddEvent(newEvent);

        // Popup schließen
        Close();
    }
}
*/