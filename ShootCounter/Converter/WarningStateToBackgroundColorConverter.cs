using ShootCounter.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShootCounter.Converter
{
    internal class WarningStateToBackgroundColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is WarningState warningState)
            {
                return warningState switch
                {
                    WarningState.Ok => Colors.Green,
                    WarningState.Warning => Colors.Red,
                    _ => throw new ArgumentException("unknown WarningState")
                };
            }

            return Colors.Yellow;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
