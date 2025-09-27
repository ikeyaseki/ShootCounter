using System.Globalization;

namespace ShootCounter.Converter
{
    public class DistanceTypeToStringConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is DistanceType distanceType)
            {
                return distanceType switch
                {
                    DistanceType.Distance25m => "25m",
                    DistanceType.Distance50m => "50m",
                    _ => throw new ArgumentException("unknown DistanceType")
                };
            }

            return "unknown";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
