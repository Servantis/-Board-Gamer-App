using BoardGamerApp.Data;
using BoardGamerApp.Services;
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

        //--Dialog Services
        builder.Services.AddSingleton<IDialogService, DialogService>();
        //--Game Library Services
        builder.Services.AddSingleton<GameDatabase>();
        builder.Services.AddTransient<GameLibraryViewModel>();
        builder.Services.AddTransient<AddGameViewModel>();
        builder.Services.AddTransient<AddGamePage>();
        builder.Services.AddTransient<GameLibrary>();



        builder.Services.AddTransient<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
