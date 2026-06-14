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