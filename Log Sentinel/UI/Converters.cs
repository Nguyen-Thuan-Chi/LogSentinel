using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Log_Sentinel.UI
{
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isEnabled)
            {
                return isEnabled ? new SolidColorBrush(Color.FromRgb(16, 185, 129)) : // Green
                                  new SolidColorBrush(Color.FromRgb(156, 163, 175));  // Gray
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class SeverityToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string severity)
            {
                return severity.ToLower() switch
                {
                    "critical" => new SolidColorBrush(Color.FromRgb(127, 29, 29)),   // Dark Red
                    "high" => new SolidColorBrush(Color.FromRgb(220, 38, 38)),       // Red
                    "medium" => new SolidColorBrush(Color.FromRgb(245, 158, 11)),    // Orange
                    "low" => new SolidColorBrush(Color.FromRgb(59, 130, 246)),       // Blue
                    _ => new SolidColorBrush(Color.FromRgb(107, 114, 128))           // Gray
                };
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
