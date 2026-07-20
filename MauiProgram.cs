using BoardGamerApp.Converters;
using BoardGamerApp.Repositories;
using BoardGamerApp.Services;
using BoardGamerApp.Services.Implementations;
using BoardGamerApp.Services.Interfaces;
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

                //Event Suggestions Services
                builder.Services.AddTransient<GameNightSuggestionsViewModel>();
                builder.Services.AddTransient<GameNightSuggestionsPage>();

                //--Database Service
                builder.Services.AddSingleton<DatabaseService>();

                //Repositories
                builder.Services.AddSingleton<BoardGameRepository>();
                builder.Services.AddSingleton<IPlayerRepository, PlayerRepository>();
                builder.Services.AddSingleton<IPlayerDeviceRepository, PlayerDeviceRepository>();
                builder.Services.AddSingleton<IGroupMemberRepository, GroupMemberRepository>();
                builder.Services.AddSingleton<IGameSuggestionRepository, GameSuggestionRepository>();
                // Transient statt Singleton: es ist okay, wenn bei Bedarf mehrfach ein
                // neues GameNightRepository-Objekt erzeugt wird, weil es selbst keinen
                // eigenen Zustand hält (nur eine Referenz auf den gemeinsamen DatabaseService).
                builder.Services.AddTransient<GameNightRepository>();

                //--Event/Termin Services
                // Alle drei werden hier registriert, damit .NET MAUI sie automatisch per
                // Konstruktor-Injection erzeugen kann, sobald sie gebraucht werden - z. B.
                // wenn per Shell.Current.GoToAsync(nameof(EventPage)) navigiert wird, baut
                // MAUI zuerst das benötigte EventViewModel (inkl. GameNightRepository,
                // BoardGameRepository, IPlayerRepository, DatabaseService) und übergibt es
                // dann automatisch dem EventPage-Konstruktor.
                builder.Services.AddTransient<EventViewModel>();
                builder.Services.AddTransient<EventPage>();
                builder.Services.AddTransient<PreviousEventsPage>();

                //--Bewertungs-Feature (RatingPage): genau wie EventPage/EventViewModel
                // braucht auch RatingPage per Konstruktor-Injection ein eigenes
                // RatingViewModel - dafür müssen beide hier als Transient registriert sein,
                // sonst kann .NET MAUI beim Navigieren zu RatingPage nicht automatisch die
                // benötigten Abhängigkeiten (DatabaseService, CurrentPlayerService) auflösen.
                builder.Services.AddTransient<RatingViewModel>();
                builder.Services.AddTransient<RatingPage>();
                builder.Services.AddSingleton<GroupOverviewRepository>();
                
                builder.Services.AddTransient<MainPage>();

                builder.Services.AddSingleton<GroupMessageRepository>();
                builder.Services.AddSingleton<GroupEmailService>();
                builder.Services.AddSingleton<GroupDelayMessageService>();

        // Für den Sync-Service
        builder.Services.AddSingleton<SyncOutboxService>();
                builder.Services.AddSingleton(new SyncApiOptions
                {
                    BaseUrl = "https://servantis.pythonanywhere.com/",
                    ApiKey = "wJmLmNaJDXL3FJuJVEixh8YG"
                });

                builder.Services.AddSingleton<HttpClient>(serviceProvider =>
                {
                    var options = serviceProvider.GetRequiredService<SyncApiOptions>();

                    return new HttpClient
                    {
                        BaseAddress = new Uri(options.BaseUrl)
                    };
                });

                builder.Services.AddSingleton<DeviceIdentityService>();
                builder.Services.AddSingleton<SyncApiClient>();
                builder.Services.AddSingleton<AppSyncService>();


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

                builder.Services.AddTransient<AddGroupPage>();
                builder.Services.AddTransient<AddGroupViewModel>();

                builder.Services.AddTransient<AddPlayerPage>();
                builder.Services.AddTransient<AddPlayerViewModel>();

                builder.Services.AddTransient<PlayerProfileViewModel>();
                builder.Services.AddTransient<PlayerProfilePage>();

                builder.Services.AddTransient<LoadingPage>();
                builder.Services.AddTransient<LoadingPageViewModel>();
                builder.Services.AddTransient<PlayerSelectionPage>();
                builder.Services.AddTransient<PlayerSelectionViewModel>();


                return builder.Build();
        }
}
