using BoardGamerApp.Converters;
using BoardGamerApp.Repositories;
using BoardGamerApp.Services;
using BoardGamerApp.Services.Implementations;
using BoardGamerApp.Services.Interfaces;
using BoardGamerApp.Services.Repositories;
using BoardGamerApp.ViewModels;
using BoardGamerApp.Views;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;



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


                //-- Appshell Services
                builder.Services.AddSingleton<AppShell>();
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

                //--Event Services
                builder.Services.AddSingleton<IsoToDisplayDateConverter>();

                //--Database Service
                builder.Services.AddSingleton<DatabaseService>();

                //Repositories
                builder.Services.AddSingleton<BoardGameRepository>();
                builder.Services.AddSingleton<IPlayerRepository, PlayerRepository>();
                builder.Services.AddSingleton<IPlayerDeviceRepository, PlayerDeviceRepository>();
                builder.Services.AddSingleton<IGroupMemberRepository, GroupMemberRepository>();
                builder.Services.AddSingleton<GroupOverviewRepository>();

                builder.Services.AddTransient<MainPage>();

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

                builder.Services.AddTransient<GroupOverviewViewModel>();
                builder.Services.AddTransient<GroupOverviewPage>();

                builder.Services.AddTransient<PlayerProfileViewModel>();
                builder.Services.AddTransient<PlayerProfilePage>();

                builder.Services.AddTransient<LoadingPage>();
                builder.Services.AddTransient<LoadingPageViewModel>();
                builder.Services.AddTransient<PlayerSelectionPage>();
                builder.Services.AddTransient<PlayerSelectionViewModel>();


                return builder.Build();
        }
}
