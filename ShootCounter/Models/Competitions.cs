
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
                    new CompetitionRun() { RunNumber = 1, Duration = 60, NumberOfBullets = 5, Einzelgezeigt = true, Name = "1. Passe 5 Schuss Einzelfeuer in je 60 Sekunden einzeln gezeigt" },
                    new CompetitionRun() { RunNumber = 2, Duration = 50, NumberOfBullets = 5, Einzelgezeigt = false, Name = "2. Passe 5 Schuss Schnellfeuer in 50 Sekunden am Schluss gezeigt" },
                    new CompetitionRun() { RunNumber = 3, Duration = 40, NumberOfBullets = 5, Einzelgezeigt = false, Name = "3. Passe 5 Schuss Schnellfeuer in 40 Sekunden am Schluss gezeigt" },
                    new CompetitionRun() { RunNumber = 4, Duration = 30, NumberOfBullets = 5, Einzelgezeigt = false, Name = "4. Passe 5 Schuss Schnellfeuer in 30 Sekunden am Schluss gezeigt" },
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
                    new CompetitionRun() { RunNumber = 1, Duration = 20, NumberOfBullets = 3, Einzelgezeigt = true, Name = "1. Passe 3 Schuss Einzelfeuer in je 20 Sekunden einzeln gezeigt" },
                    new CompetitionRun() { RunNumber = 2, Duration = 50, NumberOfBullets = 5, Einzelgezeigt = false, Name = "2. Passe 5 Schuss Schenllfeuer in 50 Sekunden am Schluss gezeigt" },
                    new CompetitionRun() { RunNumber = 3, Duration = 40, NumberOfBullets = 5, Einzelgezeigt = false, Name = "3. Passe 5 Schuss Schnellfeuer in 40 Sekunden am Schluss gezeigt" },
                    new CompetitionRun() { RunNumber = 4, Duration = 30, NumberOfBullets = 5, Einzelgezeigt = false, Name = "4. Passe 5 Schuss Schnellfeuer in 30 Sekunden am Schluss gezeigt" },
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
                    new CompetitionRun() { RunNumber = 1, Duration = 60, NumberOfBullets = 6, Einzelgezeigt = true, Name = "1. Passe 6 Schuss Einzelfeuer in je 60 Sekunden einzeln gezeigt" },
                    new CompetitionRun() { RunNumber = 2, Duration = 60, NumberOfBullets = 3, Einzelgezeigt = false, Name = "2. Passe 3 Schuss Schenllfeuer in 60 Sekunden am Schluss gezeigt" },
                    new CompetitionRun() { RunNumber = 3, Duration = 60, NumberOfBullets = 3, Einzelgezeigt = false, Name = "3. Passe 3 Schuss Schenllfeuer in 60 Sekunden am Schluss gezeigt" },
                    new CompetitionRun() { RunNumber = 4, Duration = 60, NumberOfBullets = 6, Einzelgezeigt = false, Name = "4. Passe 6 Schuss Schenllfeuer in 60 Sekunden am Schluss gezeigt" },
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
                    new CompetitionRun() { RunNumber = 1, Duration = 60, NumberOfBullets = 3, Einzelgezeigt = false, Name = "1. Passe 3 Schuss Schenllfeuer in 60 Sekunden am Schluss gezeigt" },
                    new CompetitionRun() { RunNumber = 2, Duration = 120, NumberOfBullets = 6, Einzelgezeigt = false, Name = "2. Passe 6 Schuss Schenllfeuer in 120 Sekunden am Schluss gezeigt" },
                    new CompetitionRun() { RunNumber = 3, Duration = 60, NumberOfBullets = 6, Einzelgezeigt = false, Name = "3. Passe 6 Schuss Schenllfeuer in 60 Sekunden am Schluss gezeigt" },
                }
            },
            new CompetitionProperties()
            {
                CompetitionType = CompetitionType.Morgarten,
                DistanceType = DistanceType.Distance50m,
                TargetType = TargetType.Morgarten,
                Name =  "Morgartenschiessen",
                MaxPoints = 75,
                MinPoints = null,
                MaxZeros = null,
                KartePoints = null,
                KranzPoints = null,
                TrialShots = false,
                CompetitionRuns = new List<CompetitionRun>
                {
                    new CompetitionRun() { RunNumber = 1, Duration = 30, NumberOfBullets = 2, Einzelgezeigt = false, Name = "1. Passe 2 Schuss Schenllfeuer in 30 Sekunden am Schluss gezeigt"},
                    new CompetitionRun() { RunNumber = 2, Duration = 60, NumberOfBullets = 4, Einzelgezeigt = false, Name = "2. Passe 4 Schuss Schenllfeuer in 60 Sekunden am Schluss gezeigt" },
                    new CompetitionRun() { RunNumber = 3, Duration = 60, NumberOfBullets = 6, Einzelgezeigt = false, Name = "3. Passe 6 Schuss Schenllfeuer in 60 Sekunden am Schluss gezeigt" },
                }
            },
        };
    }
}
