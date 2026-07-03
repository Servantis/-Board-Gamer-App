namespace BoardGamerApp.ViewModels;

using System.Collections.ObjectModel;
using BoardGamerApp.Models;

/// <summary>
/// Sehr einfaches, "read-only" ViewModel für PreviousEventsPage: es bekommt im
/// Konstruktor die komplette Terminliste übergeben (siehe PreviousEventsPage.xaml.cs)
/// und filtert daraus nur die Termine heraus, deren Datum in der Vergangenheit liegt.
/// Es lädt selbst nichts aus der Datenbank - das ist bereits vorher in EventViewModel
/// passiert, hier wird nur weitergefiltert.
/// </summary>
public class PreviousEventsViewModel
{
    public ObservableCollection<GameNight> PreviousEvents { get; }

    public PreviousEventsViewModel(IEnumerable<GameNight> allNights)
    {
        PreviousEvents = new ObservableCollection<GameNight>(
            allNights.Where(n => ParseDate(n.ScheduledAt) < DateTime.Now)
        );
    }

    // Gleiche Hilfsmethode wie in EventViewModel: wandelt den gespeicherten
    // ISO-8601-UTC-String zurück in ein lokales DateTime zum Vergleichen.
    private static DateTime ParseDate(string isoString)
    {
        return DateTime.Parse(
            isoString,
            null,
            System.Globalization.DateTimeStyles.RoundtripKind
        ).ToLocalTime();
    }
}
