using ShootCounter.Helpers;
using System.Globalization;

namespace ShootCounter.Converter
{
    public class CompetitionStateToStringConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is CompetitionState competitionState)
            {
                return competitionState switch
                {
                    CompetitionState.Info => "Info",
                    CompetitionState.Prepare => "Vorbereitung",
                    CompetitionState.Shooting => "Schiessen",
                    CompetitionState.CountPoints => "Punkte zählen",
                    CompetitionState.Result => "Resultat",
                    _ => throw new ArgumentException("unknown CompetitionState")
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
