namespace BoardGamerApp;

using System.Collections.ObjectModel;
using System.Windows.Input;
public class EventViewModel
{
    public ObservableCollection<BoardGameEvent> Events { get; set; }

    // Command für Klick auf ein Event
    public ICommand EventClickedCommand { get; }
    public ICommand DeleteEventCommand { get; }


    public EventViewModel()
    {
        Events = new ObservableCollection<BoardGameEvent>
        {
            new BoardGameEvent
            {
                Date = new DateTime(2026, 6, 12, 19, 0, 0),
                Location = "Annastraße 5, 12345 Musterstadt",
                Game = "Catan",
                Host = "Anna"
            },
            new BoardGameEvent
            {
                Date = new DateTime(2026, 6, 20, 18, 30, 0),
                Location = "Markusstraße 10, 12345 Musterstadt",
                Game = "Azul",
                Host = "Markus"
            }
        };

        // Command initialisieren
        EventClickedCommand = new Command<BoardGameEvent>(OnEventClicked);
        DeleteEventCommand = new Command<BoardGameEvent>(OnDeleteEvent);
    }

    public void AddEvent(BoardGameEvent newEvent)
    {
        Events.Add(newEvent);
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
            Console.WriteLine($"Event gelöscht: {evt.Game} bei {evt.Host}");
        }
    }

}
