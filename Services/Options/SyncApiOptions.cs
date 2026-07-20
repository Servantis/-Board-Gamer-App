namespace BoardGamerApp.Services;

public class SyncApiOptions
{
    public string BaseUrl { get; init; } = "https://servantis.pythonanywhere.com/";

    // Für den Test API-Key eintragen.
    // Später sollten wir das sauberer lösen
    public string ApiKey { get; init; } = "wJmLmNaJDXL3FJuJVEixh8YG";
}