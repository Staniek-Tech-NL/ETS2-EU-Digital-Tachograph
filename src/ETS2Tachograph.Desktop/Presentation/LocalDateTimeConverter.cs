using System.Globalization;
using System.Windows.Data;

namespace ETS2Tachograph.Desktop;

public sealed class LocalDateTimeConverter : IValueConverter
{
    public object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture) =>
        value switch
        {
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToLocalTime().ToString("g", culture),
            DateTime dateTime => dateTime.ToLocalTime().ToString("g", culture),
            _ => "—"
        };

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture) =>
        throw new NotSupportedException();
}
