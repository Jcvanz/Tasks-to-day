using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Tasks.App.Models;

namespace Tasks.App.Converters;

public class PriorityToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TaskPriority priority)
        {
            return priority switch
            {
                TaskPriority.Baixa => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")),    // Verde Esmeralda
                TaskPriority.Media => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6")),    // Azul
                TaskPriority.Alta => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")),     // Âmbar/Laranja
                TaskPriority.Urgente => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")),  // Vermelho
                _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280"))
            };
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class PriorityToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TaskPriority priority)
        {
            return priority switch
            {
                TaskPriority.Baixa => new SolidColorBrush(Color.FromArgb(30, 16, 185, 129)),
                TaskPriority.Media => new SolidColorBrush(Color.FromArgb(30, 59, 130, 246)),
                TaskPriority.Alta => new SolidColorBrush(Color.FromArgb(30, 245, 158, 11)),
                TaskPriority.Urgente => new SolidColorBrush(Color.FromArgb(35, 239, 68, 68)),
                _ => new SolidColorBrush(Color.FromArgb(20, 107, 114, 128))
            };
        }
        return Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class HexToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string hex && !string.IsNullOrEmpty(hex))
        {
            try
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            }
            catch
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6"));
            }
        }
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6"));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null) return Visibility.Collapsed;
        if (value is string s && string.IsNullOrWhiteSpace(s)) return Visibility.Collapsed;
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
