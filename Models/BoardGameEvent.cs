namespace BoardGamerApp;

public class BoardGameEvent
{
    public required DateTime Date { get; set; }
    public required string Location { get; set; }
    public required string Game { get; set; }
    public required string Host { get; set; }

    public string DisplayDate => Date.ToString("dddd, dd. MMMM yyyy – HH:mm 'Uhr'");
}
