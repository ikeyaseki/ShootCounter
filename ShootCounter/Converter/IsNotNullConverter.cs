using System.Globalization;

namespace ShootCounter.Converter
{
    // true when the value is set, an empty string counts as not set
    public class IsNotNullConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is string text ? !string.IsNullOrWhiteSpace(text) : value != null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
