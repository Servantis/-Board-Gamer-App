namespace BoardGamerApp.ViewModels;

using System.Collections.ObjectModel;
using BoardGamerApp.Models;

public class PreviousEventsViewModel
{
    public ObservableCollection<GameNight> PreviousEvents { get; }

    public PreviousEventsViewModel(IEnumerable<GameNight> allNights)
    {
        PreviousEvents = new ObservableCollection<GameNight>(
            allNights.Where(n => ParseDate(n.ScheduledAt) < DateTime.Now)
        );
    }

    private static DateTime ParseDate(string isoString)
    {
        return DateTime.Parse(
            isoString,
            null,
            System.Globalization.DateTimeStyles.RoundtripKind
        ).ToLocalTime();
    }
}


/*
namespace BoardGamerApp.ViewModels;

using System.Collections.ObjectModel;

public class PreviousEventsViewModel
{
    public ObservableCollection<BoardGameEvent> PreviousEvents { get; }

    public PreviousEventsViewModel(IEnumerable<BoardGameEvent> allEvents)
    {
        PreviousEvents = new ObservableCollection<BoardGameEvent>(
            allEvents.Where(e => e.Date < DateTime.Now)
        );
    }
}
*/
