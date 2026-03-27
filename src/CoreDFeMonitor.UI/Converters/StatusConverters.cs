// src/CoreDFeMonitor.UI/Converters/StatusConverters.cs
using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace CoreDFeMonitor.UI.Converters
{
    public class BoolToGreenRedConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Brushes.Green : Brushes.Red;
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    public class BoolToCienciaConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? "Ciência Automatizada (OK)" : "Sem Ciência Automatizada";
            }
            return "N/A";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}