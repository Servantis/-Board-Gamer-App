namespace BoardGamerApp.Services;

public class CurrentPlayerService
{
    public string? PlayerId { get; private set; }
    public string? PlayerName { get; private set; }
    public string? Email { get; private set; }

    public bool IsLoggedIn => !string.IsNullOrWhiteSpace(PlayerId);

    public event Action? CurrentPlayerChanged;

    public void SetPlayer(string playerId, string playerName, string? email)
    {
        PlayerId = playerId;
        PlayerName = playerName;
        Email = email;

        CurrentPlayerChanged?.Invoke();
    }

    public void Clear()
    {
        PlayerId = null;
        PlayerName = null;
        Email = null;

        CurrentPlayerChanged?.Invoke();
    }
}