using BoardGamerApp.Models;
using BoardGamerApp.Repositories;
using BoardGamerApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace BoardGamerApp.ViewModels;
public partial class MainViewModel : ObservableObject
{
    private readonly GameNightRepository _gameNightRepository;
    private readonly IGroupMemberRepository _groupMemberRepository;
    private readonly DatabaseService _databaseService;
    private readonly CurrentPlayerService _currentPlayerService;
    private readonly GroupDelayMessageService _groupDelayMessageService;

    public ObservableCollection<GameNight> UpcomingGameNights { get; } = new();

    public ObservableCollection<GroupInvitationItem> PendingInvitations { get; } = new();

    [ObservableProperty]
    private bool isBusy;

    public MainViewModel(
        GameNightRepository gameNightRepository,
        IGroupMemberRepository groupMemberRepository,
        DatabaseService databaseService,
        CurrentPlayerService currentPlayerService,
        GroupDelayMessageService groupDelayMessageService)
    {
        _gameNightRepository = gameNightRepository;
        _groupMemberRepository = groupMemberRepository;
        _databaseService = databaseService;
        _currentPlayerService = currentPlayerService;
        _groupDelayMessageService = groupDelayMessageService;
    }
}

