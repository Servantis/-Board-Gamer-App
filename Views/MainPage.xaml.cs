namespace BoardGamerApp.Views;

using BoardGamerApp.Models;
using BoardGamerApp.ViewModels;

public partial class MainPage : ContentPage
{
	public MainViewModel ViewModel { get; }

	public MainPage(MainViewModel viewModel)
	{
		InitializeComponent();
		ViewModel = viewModel;
		BindingContext = ViewModel;
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await ViewModel.LoadAsync();
    }

    // Wird angetippt, wenn auf der MainPage die Karte des nächsten Termins (NextUpcomingGameNight)
    // angeklickt wird. Springt zur EventPage (Terminverwaltung) - der Termin selbst kann dann dort
    // angetippt werden, um ihn zu bearbeiten (siehe EventPage.xaml.cs, OnEditEventClicked).
    private async void OnEventPreviewClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync(nameof(EventPage));
	}

	// Zusagen/Absagen zum nächsten Termin: "sender" ist der jeweilige Button, dessen
	// BindingContext (geerbt vom umgebenden Border, siehe MainPage.xaml) genau das
	// GameNight-Objekt ist, für das die Karte gerade angezeigt wird.
	private async void OnAcceptClicked(object? sender, EventArgs e)
	{
		if (sender is not Element element || element.BindingContext is not GameNight night)
			return;

		await ViewModel.RespondToAttendanceAsync(night, BoardGamerConstants.AttendanceStatus.Accepted);
	}

	private async void OnDeclineClicked(object? sender, EventArgs e)
	{
		if (sender is not Element element || element.BindingContext is not GameNight night)
			return;

		await ViewModel.RespondToAttendanceAsync(night, BoardGamerConstants.AttendanceStatus.Declined);
	}

	// Navigiert zur Bewertungsseite wenn auf den vergangenen Termin geklickt wird.
	private async void OnRatingClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync(nameof(RatingPage));
	}

	// Navigiert zur Eventseite wenn auf den Termine-Button geklickt wird.
	private async void OnEventClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync(nameof(EventPage));
	}

    private async void OnGroupClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(GroupOverviewPage));
    }
	// Navigiert zur Nachrichten-Seite wenn auf das Nachrichten-Symbol geklickt wird.
	private async void OnMessageClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync(nameof(MessagePage));
	}

}
