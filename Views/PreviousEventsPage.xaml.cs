namespace BoardGamerApp.Views;

public partial class PreviousEventsPage : ContentPage
{
    public PreviousEventsPage()
    {
        InitializeComponent();
    }
    private async void OnRatingClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(RatingPage));
    }
}