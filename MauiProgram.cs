using BoardGamerApp.Services.Implementations;
using BoardGamerApp.Services.Interfaces;
using BoardGamerApp.ViewModels;
using BoardGamerApp.Views;
using BoardGamerApp.Services;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using BoardGamerApp.Repositories;


namespace BoardGamerApp;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

        //--Dialog Services
        builder.Services.AddSingleton<IDialogService, DialogService>();
        //--Player and Installation Services
        builder.Services.AddSingleton<InstallationService>();
        builder.Services.AddSingleton<CurrentPlayerService>();
        //--Game Library Services
        builder.Services.AddTransient<GameLibraryViewModel>();
        builder.Services.AddTransient<AddGameViewModel>();
        builder.Services.AddTransient<AddGameView>();
        builder.Services.AddTransient<GameLibrary>();
		//--Database Service
        builder.Services.AddSingleton<DatabaseService>();

        //Repositories
        builder.Services.AddSingleton<BoardGameRepository>();
        builder.Services.AddSingleton<IPlayerRepository, PlayerRepository>();
        builder.Services.AddSingleton<IPlayerDeviceRepository, PlayerDeviceRepository>();
        builder.Services.AddSingleton<IGroupMemberRepository, GroupMemberRepository>();

        builder.Services.AddTransient<MainPage>();
        builder.Services.AddSingleton<AppShell>();

#if DEBUG
        builder.Logging.AddDebug();
        builder.Services.AddTransient<SyncOutboxDebugViewModel>();
        builder.Services.AddTransient<SyncOutboxDebugView>();
#endif
        // Service-Registration
        builder.Services.AddSingleton<IHostSelectionService, HostSelectionService>();
        builder.Services.AddSingleton<IHostScheduleService, HostScheduleService>();
        builder.Services.AddSingleton<IGameNightTrigger, SimulatedGameNightTrigger>();
        

        builder.Services.AddTransient<GroupMembersViewModel>();
        builder.Services.AddTransient<GroupPage>();
        builder.Services.AddTransient<GroupManagementPage>();

        builder.Services.AddTransient<LoadingPage>();
        builder.Services.AddTransient<LoadingPageViewModel>();
        builder.Services.AddTransient<PlayerSelectionPage>();
        builder.Services.AddTransient<PlayerSelectionViewModel>();


        return builder.Build();
	}
}
