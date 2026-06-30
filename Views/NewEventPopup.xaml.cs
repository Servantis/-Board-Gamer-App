using CommunityToolkit.Maui.Views;
using System.ComponentModel;
using BoardGamerApp.Models;

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

        HiddenDatePicker.Unfocused += OnDatePickerClosed_iOS;
        HiddenTimePicker.Unfocused += (s, e) => ApplyDateTime();
    }

    private void OnDateTimeTapped(object sender, TappedEventArgs e)
    {
        InlineDatePicker.IsVisible = true;
    }

    private void OnInlineDateSelected(object sender, DateChangedEventArgs e)
    {
        InlineTimePicker.IsVisible = true;
    }

    private void OnInlineTimeChanged(object sender, TimeChangedEventArgs e)
    {
        ApplyDateTime();
    }

    private void OnDatePickerClosed_iOS(object sender, FocusEventArgs e)
    {
        if (OperatingSystem.IsIOS())
        {
            HiddenTimePicker.Focus();
        }
    }

    private void ApplyDateTime()
    {
        DateTime date = InlineDatePicker.Date ?? DateTime.Now;
        TimeSpan time = InlineTimePicker.Time ?? new TimeSpan(12, 0, 0);

        _selectedDateTime = date + time;

        DateTimeLabel.Text = string.Format("{0:dd.MM.yyyy HH:mm}", _selectedDateTime);
        DateTimeLabel.TextColor = Colors.Black;
    }

    private void OnSaveClicked(object sender, EventArgs e)
    {
        var newNight = new GameNight
        {
            GroupId = "default", // später dynamisch
            ScheduledAt = _selectedDateTime.ToUniversalTime().ToString("o"),
            LocationId = LocationEntry.Text,
            HostPlayerId = HostEntry.Text,
            Notes = GameEntry.Text,
            Status = BoardGamerConstants.GameNightStatus.Planned
        };

        _viewModel.AddGameNight(newNight);
        Close();
    }
}


/*
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
        HiddenDatePicker.Unfocused += OnDatePickerClosed_iOS;
        HiddenTimePicker.Unfocused += (s, e) => ApplyDateTime();

    }

    private void OnDateTimeTapped(object sender, TappedEventArgs e)
    {
        InlineDatePicker.IsVisible = true;
    }

    private void OnInlineDateSelected(object sender, DateChangedEventArgs e)
    {
        InlineTimePicker.IsVisible = true;
    }

    private void OnInlineTimeChanged(object sender, TimeChangedEventArgs e)
    {
        ApplyDateTime();
    }

    private void OnDatePickerClosed_iOS(object sender, FocusEventArgs e)
    {
        // iOS: erst wechseln, wenn der Nutzer den Picker schließt
        if (OperatingSystem.IsIOS())
        {
            HiddenTimePicker.Focus();
        }
    }

    private void ApplyDateTime()
    {
        DateTime date = InlineDatePicker.Date ?? DateTime.Now;
        TimeSpan time = InlineTimePicker.Time ?? new TimeSpan(12, 0, 0);

        _selectedDateTime = date + time;

        DateTimeLabel.Text = string.Format("{0:dd.MM.yyyy HH:mm}", _selectedDateTime);
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
*/