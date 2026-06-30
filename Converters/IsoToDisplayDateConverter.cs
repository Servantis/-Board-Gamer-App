using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace BoardGamerApp.Converters;

public class IsoToDisplayDateConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string isoString && !string.IsNullOrWhiteSpace(isoString))
        {
            try
            {
                var dt = DateTime.Parse(
                    isoString,
                    null,
                    DateTimeStyles.RoundtripKind
                ).ToLocalTime();

                return dt.ToString("dd.MM.yyyy – HH:mm 'Uhr'");
            }
            catch
            {
                return "Ungültiges Datum";
            }
        }

        return "Kein Datum";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
