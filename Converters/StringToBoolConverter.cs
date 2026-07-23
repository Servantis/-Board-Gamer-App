using System.Globalization;
using Microsoft.Maui.Controls;

namespace BoardGamerApp.Converters;

/// <summary>
/// Value-Converter fürs Data Binding: wandelt einen (ggf. leeren oder null) String in
/// ein bool um. Praktisch für IsVisible-Bindings, weil MAUI dort direkt einen bool
/// erwartet, unsere Anzeige-Properties (z. B. GameNight.LocationName) aber string?
/// sind und optional/leer sein können (Ort/Veranstalter/Spiel sind ja nicht Pflicht).
///
/// Registriert als App-weite Resource in App.xaml, Verwendung z. B. in EventPage.xaml:
///   IsVisible="{Binding LocationName, Converter={StaticResource StringToBoolConverter}}"
/// -> die "Ort: ..."-Zeile wird automatisch ausgeblendet, wenn LocationName null/leer ist,
/// statt eine leere "Ort: "-Zeile anzuzeigen.
/// </summary>
public class StringToBoolConverter : IValueConverter
{
    /// <summary>true, wenn value ein nicht-leerer String ist - sonst false.</summary>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is string text && !string.IsNullOrWhiteSpace(text);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
