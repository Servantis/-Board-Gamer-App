using BoardGamerApp.Models;
using BoardGamerApp.Repositories;
using BoardGamerApp.Services;
using BoardGamerApp.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.RegularExpressions;

namespace BoardGamerApp.ViewModels;
public partial class MainViewModel : ObservableObject
{

    private readonly EventViewModel _eventViewModel;
    private readonly IGroupMemberRepository _groupMemberRepository;
    private readonly CurrentPlayerService _currentPlayerService;
    private readonly GroupDelayMessageService _groupDelayMessageService;

    public ObservableCollection<GameNight> UpcomingGameNights { get; } = new();

    public ObservableCollection<GroupInvitationItem> PendingInvitations { get; } = new();

    [ObservableProperty]
    private bool isBusy;

    public MainViewModel(
        IGroupMemberRepository groupMemberRepository,
        CurrentPlayerService currentPlayerService,
        EventViewModel eventViewModel,
        GroupDelayMessageService groupDelayMessageService)
    {
        _groupMemberRepository = groupMemberRepository;
        _currentPlayerService = currentPlayerService;
        _eventViewModel = eventViewModel;
        _groupDelayMessageService = groupDelayMessageService;
    }

    // Navigation zur GameNightSuggestionPage
    [RelayCommand]
    private async Task OpenSuggestionsAsync(GameNight? night)
    {
        if (night is null)
            return;

        await Shell.Current.GoToAsync(
            nameof(GameNightSuggestionsPage),
            new Dictionary<string, object>
            {
            { "GameNight", night }
            });
    }

    // Verspätungsmittelung senden
    [RelayCommand]
    private async Task SendDelayMessageAsync(GameNight? night)
    {
        if (night is null)
            return;

        var currentPlayerId =
            _currentPlayerService.PlayerId;

        if (string.IsNullOrWhiteSpace(currentPlayerId))
            return;

        var selectedDelay =
            await Shell.Current.DisplayActionSheetAsync(
                "Verspätung melden",
                "Abbrechen",
                null,
                "5 Minuten",
                "10 Minuten",
                "15 Minuten",
                "30 Minuten");

        if (string.IsNullOrWhiteSpace(selectedDelay) ||
            selectedDelay == "Abbrechen")
        {
            return;
        }

        var match =
            Regex.Match(selectedDelay, @"\d+");

        if (!match.Success)
            return;

        var delayMinutes =
            int.Parse(match.Value);

        await _groupDelayMessageService
            .SendDelayMessageToGroupAsync(
                night.GroupId,
                currentPlayerId,
                delayMinutes);
    }
    public async Task LoadAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;

            await LoadInvitationsAsync();
            await LoadUpcomingEventsAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Der chronologisch nächste anstehende Termin, an dem der aktuelle Spieler auch
    /// TATSÄCHLICH teilnimmt, oder null, falls keiner existiert - für die
    /// "Nächster Termin"-Vorschau-Karte auf der MainPage. Das ist entweder ein Termin,
    /// den der Spieler selbst hostet (er nimmt als Gastgeber automatisch teil), oder
    /// einer, dem er bereits zugesagt hat (siehe GameNight.MyAttendanceStatus). Termine
    /// mit einer noch offenen oder abgelehnten Antwort tauchen hier bewusst NICHT auf -
    /// dafür gibt es die separate "Noch offen"-Karte (siehe NextUnansweredGameNight).
    /// Abgesagte Termine (Status "cancelled", siehe GameNight.IsCancelled) werden
    /// ebenfalls übersprungen.
    /// </summary>
    public GameNight? NextUpcomingGameNight =>
        UpcomingGameNights
            .Where(n => !n.IsCancelled
                && (n.IsHostedByCurrentPlayer
                    || n.MyAttendanceStatus == BoardGamerConstants.AttendanceStatus.Accepted))
            .OrderBy(n => ParseDate(n.ScheduledAt))
            .FirstOrDefault();

    /// <summary>
    /// True, wenn es einen anzeigbaren nächsten Termin gibt (für IsVisible-Bindings auf
    /// der MainPage) - bewusst über NextUpcomingGameNight statt über die reine Anzahl von
    /// UpcomingGameNights bestimmt, damit die Karte automatisch verschwindet, wenn alle
    /// künftigen Termine abgesagt sind.
    /// </summary>
    public bool HasUpcomingEvents => NextUpcomingGameNight is not null;

    /// <summary>
    /// Der chronologisch nächste anstehende Termin, zu dem der aktuelle Spieler ALS GAST
    /// (also explizit NICHT als Gastgeber) noch GAR NICHT geantwortet hat - für die zweite
    /// Vorschau-Karte auf der MainPage, die gezielt an eine noch offene Zusage/Absage
    /// erinnert. Der eigene Gastgeber ist hier bewusst DOPPELT ausgeschlossen
    /// (!IsHostedByCurrentPlayer zusätzlich zu CanRespondToAttendance, das dieselbe
    /// Bedingung eigentlich schon enthält) - so landet garantiert nie ein selbst gehosteter
    /// Termin auf dieser Karte, für den es ja ohnehin nichts zu entscheiden gibt.
    ///
    /// Ist NextUpcomingGameNight selbst bereits so ein Termin (dort steht ja ohnehin schon
    /// Zusagen/Absagen), wird er hier bewusst übersprungen, damit nicht zweimal dieselbe
    /// Karte erscheint - stattdessen zeigt diese Property dann den NÄCHSTEN Termin danach,
    /// der noch eine Antwort braucht.
    /// </summary>
    public GameNight? NextUnansweredGameNight =>
        UpcomingGameNights
            .Where(n => !n.IsHostedByCurrentPlayer
                && n.CanRespondToAttendance
                && string.IsNullOrWhiteSpace(n.MyAttendanceStatus))
            .Where(n => NextUpcomingGameNight is null || n.Id != NextUpcomingGameNight.Id)
            .OrderBy(n => ParseDate(n.ScheduledAt))
            .FirstOrDefault();

    /// <summary>
    /// True, wenn es einen Termin gibt, auf den NextUnansweredGameNight zutrifft (für
    /// IsVisible-Bindings auf der MainPage).
    /// </summary>
    public bool HasUnansweredEvent => NextUnansweredGameNight is not null;

    // Hat der aktuelle Spieler noch ausstehende Einladungen zu Gruppen
    public bool HasPendingInvitations =>
        PendingInvitations.Any();

    // Lädt die Einladungen, die der aktuelle Spieler noch nicht angenommen oder abgelehnt hat,
    // und aktualisiert die PendingInvitations-Collection.
    private async Task LoadInvitationsAsync()
    {
        PendingInvitations.Clear();

        var currentPlayerId = _currentPlayerService.PlayerId;

        if (string.IsNullOrWhiteSpace(currentPlayerId))
            return;

        var invitations =
            await _groupMemberRepository
                .GetPendingInvitationsAsync(currentPlayerId);

        foreach (var invitation in invitations)
        {
            PendingInvitations.Add(invitation);
        }

        OnPropertyChanged(nameof(HasPendingInvitations));
    }

    private async Task LoadUpcomingEventsAsync()
    {
        UpcomingGameNights.Clear();

        var nights =
            await _eventViewModel.GetPreparedGameNightsAsync();

        foreach (var night in nights)
        {
            if (ParseDate(night.ScheduledAt) >= DateTime.Now)
            {
                UpcomingGameNights.Add(night);
            }
        }

        NotifyDerivedProperties();
    }

    /// <summary>
    /// Wandelt den in der Datenbank gespeicherten ISO-8601-UTC-String (siehe
    /// GameNight.ScheduledAt) zurück in ein lokales DateTime, damit man z. B.
    /// "liegt der Termin in der Vergangenheit?" mit DateTime.Now vergleichen kann.
    /// </summary>
    private static DateTime ParseDate(string isoString)
    {
        return DateTime.Parse(
            isoString,
            null,
            System.Globalization.DateTimeStyles.RoundtripKind)
            .ToLocalTime();
    }

    private void NotifyDerivedProperties()
    {
        OnPropertyChanged(nameof(NextUpcomingGameNight));
        OnPropertyChanged(nameof(HasUpcomingEvents));
        OnPropertyChanged(nameof(NextUnansweredGameNight));
        OnPropertyChanged(nameof(HasUnansweredEvent));
        OnPropertyChanged(nameof(HasPendingInvitations));
    }

    public async Task RespondToAttendanceAsync(
        GameNight night,
        string status)
    {
        await _eventViewModel
            .RespondToAttendanceAsync(
                night,
                status);

        await LoadUpcomingEventsAsync();
    }

    // Gruppen-Eingeladung wurde angenommen, der Status der Person wird auf 'active' gesetzt
    [RelayCommand]
    private async Task AcceptInvitationAsync(
    GroupInvitationItem? invitation)
    {
        if (invitation is null)
            return;

        try
        {
            var member =
                await _groupMemberRepository
                    .GetMemberByIdAsync(invitation.MemberId);

            if (member is null)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Fehler",
                    "Die Einladung wurde nicht gefunden.",
                    "OK");
                return;
            }

            member.Status =
                BoardGamerConstants.GroupMemberStatus.Active;

            await _groupMemberRepository
                .UpdateMemberAsync(member);

            await LoadInvitationsAsync();

            await Shell.Current.DisplayAlertAsync(
                "Gruppe beigetreten",
                $"Du bist jetzt Mitglied der Gruppe \"{invitation.GroupName}\".",
                "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(
                "Fehler",
                ex.Message,
                "OK");
        }
    }

    // Die Gruppen-Einladung wurde abgelehnt, der Status der eingeladenen Person wird auf 'left' gesetzt
    [RelayCommand]
    private async Task DeclineInvitationAsync(
    GroupInvitationItem? invitation)
    {
        if (invitation is null)
            return;

        try
        {
            var member =
                await _groupMemberRepository
                    .GetMemberByIdAsync(invitation.MemberId);

            if (member is null)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Fehler",
                    "Die Einladung wurde nicht gefunden.",
                    "OK");
                return;
            }

            member.Status =
                BoardGamerConstants.GroupMemberStatus.Left;

            await _groupMemberRepository
                .UpdateMemberAsync(member);

            await LoadInvitationsAsync();

            await Shell.Current.DisplayAlertAsync(
                "Einladung abgelehnt",
                $"Die Einladung für \"{invitation.GroupName}\" wurde abgelehnt.",
                "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(
                "Fehler",
                ex.Message,
                "OK");
        }
    }
}

