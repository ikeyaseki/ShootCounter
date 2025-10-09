using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShootCounter.Converter
{
    public class CompetitionTypeToInfoStringConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is CompetitionType competitionType)
            {
                if (competitionType == CompetitionType.Unknown)
                    return "unknown";

                var competitionProperties = Competitions.CompetitionProperties.First(n => n.CompetitionType == competitionType);
                var targetProperties = Targets.TargetProperties.First(n => n.TargetType == competitionProperties.TargetType);

                var infoString = string.Empty;
                DistanceTypeToStringConverter distanceTypeToStringConverter = new DistanceTypeToStringConverter();
                
                infoString = $"Distanz: {distanceTypeToStringConverter.Convert(competitionProperties.DistanceType, typeof(DistanceType), null, culture)}\n";
                infoString = $"{infoString}Scheibe: {targetProperties.Name}\n";

                var trials = "Probeschüsse: ";
                trials = competitionProperties.TrialShots ? $"{trials} Ja" : $"{trials} Keine";
                infoString = $"{infoString}{trials}\n";

                foreach (var run in competitionProperties.CompetitionRuns)
                {
                    infoString = $"{infoString}{run.Name}\n";
                }
                
                infoString = $"{infoString}Maximale Punktzahl: {competitionProperties.MaxPoints}\n";

                if (competitionProperties.MinPoints != null)
                    infoString = $"{infoString}Mindest Punktzahl: {competitionProperties.MinPoints}\n";

                if (competitionProperties.MaxZeros != null)
                    infoString = $"{infoString}Maximale Fehlschüsse: {competitionProperties.MaxZeros}\n";

                if (competitionProperties.KartePoints != null)
                    infoString = $"{infoString}Punktzahl für Karte: {competitionProperties.KartePoints}\n";

                if (competitionProperties.KranzPoints != null)
                    infoString = $"{infoString}Punktzahl für Kranz: {competitionProperties.KranzPoints}\n";

                return infoString;
            }

            return "unknown";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
