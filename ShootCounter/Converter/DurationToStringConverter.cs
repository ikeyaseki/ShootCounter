using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShootCounter.Converter
{
    public class DurationToStringConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is TimeSpan timeSpan && timeSpan > TimeSpan.Zero)
            {
                var minutes = Math.Round(timeSpan.TotalMinutes, 0);
                if (minutes == 0)
                    minutes = 1;
                if (minutes == 1)
                    return $"{minutes} Minute";

                return $"{minutes} Minuten";
            }

            return "unbekannt";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
