
namespace ShootCounter.Models
{
    public static class Competitions
    {
        public static IEnumerable<CompetitionProperties> CompetitionProperties { get; set; } = new List<CompetitionProperties>
        {
            new CompetitionProperties()
            {
                CompetitionType = CompetitionType.Obligatorisch25m,
                DistanceType = DistanceType.Distance25m,
                TargetType = TargetType.OrderlyRapidFire,
                Name =  "Obligatorisches Bundesprogramm 25m",
                MaxPoints = 200,
                MinPoints = 120,
                MaxZeros = 3,
                KartePoints = 175,
                KranzPoints = null,
                TrialShots = true,
                CompetitionRuns = new List<CompetitionRun>
                {
                    new CompetitionRun() { RunNumber = 1, Duration = 60, NumberOfBullets = 5, Einzelgezeigt = true, Name = "1.\tPasse 5 Schuss Einzelfeuer\n\t\t\tin je 60 Sekunden einzeln gezeigt" },
                    new CompetitionRun() { RunNumber = 2, Duration = 50, NumberOfBullets = 5, Einzelgezeigt = false, Name = "2.\tPasse 5 Schuss Schnellfeuer\n\t\t\tin 50 Sekunden am Schluss gezeigt" },
                    new CompetitionRun() { RunNumber = 3, Duration = 40, NumberOfBullets = 5, Einzelgezeigt = false, Name = "3.\tPasse 5 Schuss Schnellfeuer\n\t\t\tin 40 Sekunden am Schluss gezeigt" },
                    new CompetitionRun() { RunNumber = 4, Duration = 30, NumberOfBullets = 5, Einzelgezeigt = false, Name = "4.\tPasse 5 Schuss Schnellfeuer\n\t\t\tin 30 Sekunden am Schluss gezeigt" },
                }
            },
            new CompetitionProperties()
            {
                CompetitionType = CompetitionType.Feldschiessen25m,
                DistanceType = DistanceType.Distance25m,
                TargetType = TargetType.OrderlyRapidFire,
                Name =  "Feldschiessen 25m",
                MaxPoints = 180,
                MinPoints = null,
                MaxZeros = null,
                KartePoints = 153,
                KranzPoints = 159,
                TrialShots = false,
                CompetitionRuns = new List<CompetitionRun>
                {
                    new CompetitionRun() { RunNumber = 1, Duration = 20, NumberOfBullets = 3, Einzelgezeigt = true, Name = "1.\tPasse 3 Schuss Einzelfeuer\n\t\t\tin je 20 Sekunden einzeln gezeigt" },
                    new CompetitionRun() { RunNumber = 2, Duration = 50, NumberOfBullets = 5, Einzelgezeigt = false, Name = "2.\tPasse 5 Schuss Schnellfeuer\n\t\t\tin 50 Sekunden am Schluss gezeigt" },
                    new CompetitionRun() { RunNumber = 3, Duration = 40, NumberOfBullets = 5, Einzelgezeigt = false, Name = "3.\tPasse 5 Schuss Schnellfeuer\n\t\t\tin 40 Sekunden am Schluss gezeigt" },
                    new CompetitionRun() { RunNumber = 4, Duration = 30, NumberOfBullets = 5, Einzelgezeigt = false, Name = "4.\tPasse 5 Schuss Schnellfeuer\n\t\t\tin 30 Sekunden am Schluss gezeigt" },
                }
            },
            new CompetitionProperties()
            {
                CompetitionType = CompetitionType.Feldschiessen50m,
                DistanceType = DistanceType.Distance50m,
                TargetType = TargetType.CombinationTargetB5,
                Name =  "Feldschiessen 50m",
                MaxPoints = 90,
                MinPoints = null,
                MaxZeros = null,
                KartePoints = 58,
                KranzPoints = 63,
                TrialShots = false,
                CompetitionRuns = new List<CompetitionRun>
                {
                    new CompetitionRun() { RunNumber = 1, Duration = 60, NumberOfBullets = 6, Einzelgezeigt = true, Name = "1.\tPasse 6 Schuss Einzelfeuer\n\t\t\tin je 60 Sekunden einzeln gezeigt" },
                    new CompetitionRun() { RunNumber = 2, Duration = 60, NumberOfBullets = 3, Einzelgezeigt = false, Name = "2.\tPasse 3 Schuss Schnellfeuer\n\t\t\tin 60 Sekunden am Schluss gezeigt" },
                    new CompetitionRun() { RunNumber = 3, Duration = 60, NumberOfBullets = 3, Einzelgezeigt = false, Name = "3.\tPasse 3 Schuss Schnellfeuer\n\t\t\tin 60 Sekunden am Schluss gezeigt" },
                    new CompetitionRun() { RunNumber = 4, Duration = 60, NumberOfBullets = 6, Einzelgezeigt = false, Name = "4.\tPasse 6 Schuss Schnellfeuer\n\t\t\tin 60 Sekunden am Schluss gezeigt" },
                }
            },
            new CompetitionProperties()
            {
                CompetitionType = CompetitionType.Ruetli,
                DistanceType = DistanceType.Distance50m,
                TargetType = TargetType.CombinationTargetB5,
                Name =  "Rütlischiessen",
                MaxPoints = 75,
                MinPoints = null,
                MaxZeros = null,
                KartePoints = null,
                KranzPoints = null,
                TrialShots = false,
                CompetitionRuns = new List<CompetitionRun>
                {
                    new CompetitionRun() { RunNumber = 1, Duration = 60, NumberOfBullets = 3, Einzelgezeigt = false, Name = "1.\tPasse 3 Schuss Schnellfeuer\n\t\t\tin 60 Sekunden am Schluss gezeigt" },
                    new CompetitionRun() { RunNumber = 2, Duration = 120, NumberOfBullets = 6, Einzelgezeigt = false, Name = "2.\tPasse 6 Schuss Schnellfeuer\n\t\t\tin 120 Sekunden am Schluss gezeigt" },
                    new CompetitionRun() { RunNumber = 3, Duration = 60, NumberOfBullets = 6, Einzelgezeigt = false, Name = "3.\tPasse 6 Schuss Schnellfeuer\n\t\t\tin 60 Sekunden am Schluss gezeigt" },
                }
            },
            new CompetitionProperties()
            {
                CompetitionType = CompetitionType.Morgarten,
                DistanceType = DistanceType.Distance50m,
                TargetType = TargetType.Morgarten,
                Name =  "Morgartenschiessen",
                MaxPoints = 60,
                MinPoints = null,
                MaxZeros = null,
                KartePoints = null,
                KranzPoints = null,
                TrialShots = false,
                CompetitionRuns = new List<CompetitionRun>
                {
                    new CompetitionRun() { RunNumber = 1, Duration = 30, NumberOfBullets = 2, Einzelgezeigt = false, Name = "1.\tPasse 2 Schuss Schnellfeuer\n\t\t\tin 30 Sekunden am Schluss gezeigt"},
                    new CompetitionRun() { RunNumber = 2, Duration = 60, NumberOfBullets = 4, Einzelgezeigt = false, Name = "2.\tPasse 4 Schuss Schnellfeuer\n\t\t\tin 60 Sekunden am Schluss gezeigt" },
                    new CompetitionRun() { RunNumber = 3, Duration = 60, NumberOfBullets = 6, Einzelgezeigt = false, Name = "3.\tPasse 6 Schuss Schnellfeuer\n\t\t\tin 60 Sekunden am Schluss gezeigt" },
                }
            },
        };
    }
}
