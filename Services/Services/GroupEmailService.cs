using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.Communication;

namespace BoardGamerApp.Services;

public class GroupEmailService
{
    public async Task ComposeDelayMessageAsync(
        IReadOnlyList<string> recipients,
        string senderName,
        int delayMinutes,
        string? customMessage = null)
    {
        var cleanedRecipients = recipients
            .Where(email => !string.IsNullOrWhiteSpace(email))
            .Select(email => email.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (cleanedRecipients.Count == 0)
        {
            throw new InvalidOperationException(
                "Es wurden keine E-Mail-Adressen für die aktiven Gruppenmitglieder gefunden.");
        }

        if (!Email.Default.IsComposeSupported)
        {
            throw new InvalidOperationException(
                "Auf diesem Gerät ist kein E-Mail-Client zum Verfassen von E-Mails verfügbar. " +
                "Bitte prüfe, ob z. B. Gmail/Outlook installiert und ein Konto eingerichtet ist.");
        }

        var subject = "Verspätung zum Spieleabend";
        var body = BuildDelayMessageBody(senderName, delayMinutes, customMessage);

        var message = new EmailMessage
        {
            Subject = subject,
            Body = body,
            BodyFormat = EmailBodyFormat.PlainText,
            To = cleanedRecipients
        };

        try
        {
            // ComposeAsync öffnet eine native App/Activity.
            // Auf Android/iOS sollte dieser Aufruf sicher auf dem UI-Thread passieren.
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Email.Default.ComposeAsync(message);
            });
        }
        catch (FeatureNotSupportedException ex)
        {
            throw new InvalidOperationException(
                "Auf diesem Gerät ist das Öffnen des E-Mail-Clients nicht unterstützt.",
                ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Der E-Mail-Client konnte nicht geöffnet werden. " +
                "Prüfe bitte, ob eine E-Mail-App installiert und eingerichtet ist und ob AndroidManifest.xml/Info.plist angepasst wurden.",
                ex);
        }
    }

    private static string BuildDelayMessageBody(
        string senderName,
        int delayMinutes,
        string? customMessage)
    {
        var displayName = string.IsNullOrWhiteSpace(senderName)
            ? "Ich"
            : senderName.Trim();

        if (!string.IsNullOrWhiteSpace(customMessage))
        {
            return
                $"Hallo zusammen,\n\n" +
                $"{customMessage.Trim()}\n\n" +
                $"Viele Grüße\n" +
                $"{displayName}";
        }

        var delayText = delayMinutes > 0
            ? $"ca. {delayMinutes} Minuten später"
            : "etwas später";

        return
            $"Hallo zusammen,\n\n" +
            $"ich verspäte mich leider und komme voraussichtlich {delayText}.\n\n" +
            $"Viele Grüße\n" +
            $"{displayName}";
    }
}
