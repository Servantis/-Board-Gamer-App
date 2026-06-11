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
