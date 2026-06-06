using CommunityToolkit.Maui.Views;

namespace BoardGamerApp.Views;

public partial class NewEventPopup : Popup
{
    private readonly EventViewModel _viewModel;

    public NewEventPopup(EventViewModel vm)
    {
        InitializeComponent();
        _viewModel = vm;
    }

    private void OnSaveClicked(object sender, EventArgs e)
    {
        var newEvent = new BoardGameEvent
        {
            Date = DateTime.Parse(DateEntry.Text),
            Location = LocationEntry.Text,
            Game = GameEntry.Text,
            Host = HostEntry.Text
        };

        _viewModel.AddEvent(newEvent);
        Close();
    }
}




/*
namespace BoardGamerApp.Views;

public partial class NewEventPopup : Popup
{
    private readonly EventViewModel _viewModel;

    public NewEventPopup(EventViewModel vm)
    {
        InitializeComponent();
        this.ShowPopup(new NewEventPopup(ViewModel));

    }

    private void OnSaveClicked(object sender, EventArgs e)
    {
        var newEvent = new BoardGameEvent
        {
            Date = DateTime.Parse(DateEntry.Text),
            Location = LocationEntry.Text,
            Game = GameEntry.Text,
            Host = HostEntry.Text
        };

        _viewModel.AddEvent(newEvent);
        Close();
    }
}



public partial class NewEventPopup : Popup
{
    public NewEventPopup()
    {
        InitializeComponent();
    }
}
*/