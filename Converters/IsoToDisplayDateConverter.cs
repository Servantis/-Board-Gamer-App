using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace BoardGamerApp.Converters;

/// <summary>
/// Value-Converter fürs Data Binding: rechnet den in der Datenbank gespeicherten
/// ISO-8601-UTC-String (z. B. "2026-07-12T17:00:00.000Z", siehe GameNight.ScheduledAt)
/// in ein für Menschen lesbares Format um ("12.07.2026 – 17:00 Uhr").
///
/// Registriert als App-weite Resource in App.xaml:
///   &lt;converters:IsoToDisplayDateConverter x:Key="IsoToDisplayDateConverter"/&gt;
/// und wird in XAML so verwendet:
///   Text="{Binding ScheduledAt, Converter={StaticResource IsoToDisplayDateConverter}}"
///
/// Warum überhaupt ein Converter? Weil ScheduledAt als reiner String gespeichert
/// wird (SQLite kennt kein Datums-Format) - direkt gebunden würde man also den
/// technischen ISO-String sehen statt ein schönes Datum. Der Converter übernimmt
/// nur die Umwandlung für die Anzeige, das GameNight-Objekt selbst bleibt unverändert.
/// </summary>
public class IsoToDisplayDateConverter : IValueConverter
{
    /// <summary>Wird beim Anzeigen aufgerufen: String aus der DB -> lesbarer Text für die UI.</summary>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string isoString && !string.IsNullOrWhiteSpace(isoString))
        {
            try
            {
                // DateTimeStyles.RoundtripKind sorgt dafür, dass die im String enthaltene
                // Zeitzoneninfo (das "Z" für UTC) korrekt erkannt wird.
                var dt = DateTime.Parse(
                    isoString,
                    null,
                    DateTimeStyles.RoundtripKind
                ).ToLocalTime();

                return dt.ToString("dd.MM.yyyy – HH:mm 'Uhr'");
            }
            catch
            {
                // Falls der String aus irgendeinem Grund kein gültiges Datum ist,
                // soll die App nicht abstürzen, sondern nur einen Hinweistext zeigen.
                return "Ungültiges Datum";
            }
        }

        return "Kein Datum";
    }

    /// <summary>
    /// Wird für die Rückrichtung (UI -> ViewModel) gebraucht, z. B. bei TwoWay-Bindings.
    /// Brauchen wir hier nicht, weil ScheduledAt nur angezeigt, nicht direkt über
    /// dieses Binding bearbeitet wird - deshalb wirft diese Methode bewusst eine Exception.
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
