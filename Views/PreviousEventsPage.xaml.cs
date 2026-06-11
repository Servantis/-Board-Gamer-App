namespace BoardGamerApp.Views;

using System.Collections.ObjectModel;
using BoardGamerApp.ViewModels;

[QueryProperty(nameof(Events), "Events")]
public partial class PreviousEventsPage : ContentPage
{
    public ObservableCollection<BoardGameEvent> Events
    {
        set
        {
            BindingContext = new PreviousEventsViewModel(value);
        }
    }

    public PreviousEventsPage()
    {
        InitializeComponent();
    }

    private async void OnRatingClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(RatingPage));
    }
}