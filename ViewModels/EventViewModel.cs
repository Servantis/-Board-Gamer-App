namespace BoardGamerApp;

using System.Collections.ObjectModel;
using System.Windows.Input;
using BoardGamerApp.Models;

public class EventViewModel
{
    public ObservableCollection<GameNight> GameNights { get; set; }
    public ObservableCollection<GameNight> UpcomingGameNights { get; set; }

    public ICommand EventClickedCommand { get; }
    public ICommand DeleteEventCommand { get; }

    // Hilfs-Property: Vergangene Events
    public IEnumerable<GameNight> PastGameNights
        => GameNights.Where(n => ParseDate(n.ScheduledAt) < DateTime.Now);

    public EventViewModel()
    {
        // Beispiel-Daten (nur Platzhalter, später aus DB laden)
        GameNights = new ObservableCollection<GameNight>
        {
            new GameNight
            {
                GroupId = "default",
                ScheduledAt = new DateTime(2026, 6, 12, 19, 0, 0).ToUniversalTime().ToString("o"),
                LocationId = "Annastraße 67",
                HostPlayerId = "Anna",
                Notes = "Catan"
            },
            new GameNight
            {
                GroupId = "default",
                ScheduledAt = new DateTime(2026, 6, 16, 19, 0, 0).ToUniversalTime().ToString("o"),
                LocationId = "Horststraße 17",
                HostPlayerId = "Horst",
                Notes = "Schach"
            },
            new GameNight
            {
                GroupId = "default",
                ScheduledAt = new DateTime(2026, 6, 17, 19, 0, 0).ToUniversalTime().ToString("o"),
                LocationId = "Mannistraße 17",
                HostPlayerId = "Manni",
                Notes = "Skat"
            }
        };

        UpcomingGameNights = new ObservableCollection<GameNight>(
            GameNights.Where(n => ParseDate(n.ScheduledAt) >= DateTime.Now));

        EventClickedCommand = new Command<GameNight>(OnEventClicked);
        DeleteEventCommand = new Command<GameNight>(OnDeleteEvent);
    }

    public void AddGameNight(GameNight night)
    {
        GameNights.Add(night);

        if (ParseDate(night.ScheduledAt) >= DateTime.Now)
            UpcomingGameNights.Add(night);
    }

    private void OnEventClicked(GameNight night)
    {
        Console.WriteLine(
            $"Event angeklickt: {night.Notes} bei {night.HostPlayerId} am {ParseDate(night.ScheduledAt)}"
        );
    }

    private void OnDeleteEvent(GameNight night)
    {
        if (night != null && GameNights.Contains(night))
        {
            GameNights.Remove(night);

            if (UpcomingGameNights.Contains(night))
                UpcomingGameNights.Remove(night);

            Console.WriteLine(
                $"Event gelöscht: {night.Notes} bei {night.HostPlayerId}"
            );
        }
    }

    public IEnumerable<GameNight> Top3UpcomingGameNights =>
        UpcomingGameNights
            .OrderBy(n => ParseDate(n.ScheduledAt))
            .Take(3);

    // Hilfsmethode: ISO-String → DateTime
    private static DateTime ParseDate(string isoString)
    {
        return DateTime.Parse(isoString, null, System.Globalization.DateTimeStyles.RoundtripKind)
                       .ToLocalTime();
    }
}


/*
namespace BoardGamerApp;

using System.Collections.ObjectModel;
using System.Windows.Input;

public class EventViewModel
{
    public ObservableCollection<BoardGameEvent> Events { get; set; }
    public ObservableCollection<BoardGameEvent> UpcomingEvents { get; set; }

    public ICommand EventClickedCommand { get; }
    public ICommand DeleteEventCommand { get; }

    public IEnumerable<BoardGameEvent> PastEvents
        => Events.Where(e => e.Date < DateTime.Now);

    public EventViewModel()
    {
        Events = new ObservableCollection<BoardGameEvent>
        {
            new BoardGameEvent
            {
                Date = new DateTime(2026, 6, 12, 19, 0, 0),
                Location = "Annastraße 67, 12345 Musterstadt",
                Game = "Catan",
                Host = "Anna"
            },
            new BoardGameEvent
            {
                Date = new DateTime(2026, 6, 16, 19, 0, 0),
                Location = "Horststraße 17, 12345 Musterstadt",
                Game = "Schach",
                Host = "Horst"
            },
            new BoardGameEvent
            {
                Date = new DateTime(2026, 6, 17, 19, 0, 0),
                Location = "Mannistraße 17, 12345 Musterstadt",
                Game = "Skat",
                Host = "Manni"
            },
            new BoardGameEvent
            {
                Date = new DateTime(2026, 6, 18, 19, 0, 0),
                Location = "Dieterstraße 17, 12345 Musterstadt",
                Game = "Mensch ärgere dich nicht",
                Host = "Dieter"
            },
            new BoardGameEvent
            {
                Date = new DateTime(2026, 6, 20, 18, 30, 0),
                Location = "Markusstraße 10, 12345 Musterstadt",
                Game = "Azul",
                Host = "Markus"
            }
        };

        UpcomingEvents = new ObservableCollection<BoardGameEvent>(
            Events.Where(e => e.Date >= DateTime.Now));

        EventClickedCommand = new Command<BoardGameEvent>(OnEventClicked);
        DeleteEventCommand = new Command<BoardGameEvent>(OnDeleteEvent);
    }

    public void AddEvent(BoardGameEvent newEvent)
    {
        Events.Add(newEvent);

        if (newEvent.Date >= DateTime.Now)
            UpcomingEvents.Add(newEvent);
    }

    private void OnEventClicked(BoardGameEvent evt)
    {
        Console.WriteLine($"Event angeklickt: {evt.Game} bei {evt.Host} am {evt.Date}");
    }

    private void OnDeleteEvent(BoardGameEvent evt)
    {
        if (evt != null && Events.Contains(evt))
        {
            Events.Remove(evt);

            if (UpcomingEvents.Contains(evt))
                UpcomingEvents.Remove(evt);

            Console.WriteLine($"Event gelöscht: {evt.Game} bei {evt.Host}");
        }
    }

    public IEnumerable<BoardGameEvent> Top3UpcomingEvents =>
    UpcomingEvents.OrderBy(e => e.Date).Take(3);

}
*/