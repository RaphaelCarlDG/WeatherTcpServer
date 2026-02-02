using System;
using System.Globalization;
using System.Windows.Data;

namespace WeatherTcpServer.Dashboard.Converters;

public sealed class ReadingTimeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime dt)
        {
            if (dt == default)
            {
                return "--";
            }

            return dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        return "--";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
