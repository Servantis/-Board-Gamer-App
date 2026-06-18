using BoardGamerApp.Data;
using BoardGamerApp.Services;
using BoardGamerApp.Services.Interfaces;
using BoardGamerApp.ViewModels;
using BoardGamerApp.Views;
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
        //--Game Library Services
        builder.Services.AddTransient<GameLibraryViewModel>();
        builder.Services.AddTransient<AddGameViewModel>();
        builder.Services.AddTransient<AddGameView>();
        builder.Services.AddTransient<GameLibrary>();
		//--Database Service
        builder.Services.AddSingleton<DatabaseService>();

        //Repositories
        builder.Services.AddSingleton<BoardGameRepository>();

        builder.Services.AddTransient<MainPage>();
        builder.Services.AddSingleton<AppShell>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
