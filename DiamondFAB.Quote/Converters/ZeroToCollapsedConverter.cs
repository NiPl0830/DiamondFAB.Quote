using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DiamondFAB.Quote.Converters
{
    /// <summary>
    /// Returns Collapsed when the bound value is null, not a number, or <= 0; otherwise Visible.
    /// Works with double, decimal, int, string (parsed), etc.
    /// </summary>
    public sealed class ZeroToCollapsedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double d;
            if (value == null) return Visibility.Collapsed;

            if (value is double dd) d = dd;
            else if (value is decimal dec) d = (double)dec;
            else if (value is int ii) d = ii;
            else if (value is long ll) d = ll;
            else if (value is float ff) d = ff;
            else if (value is string s && double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
                d = parsed;
            else
                return Visibility.Collapsed;

            return d > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
