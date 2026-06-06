namespace BoardGamerApp;

using System.Collections.ObjectModel;

public class EventViewModel
{
    public ObservableCollection<BoardGameEvent> Events { get; set; }

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
    }

    public void AddEvent(BoardGameEvent newEvent)
    {
        Events.Add(newEvent);
    }
}
