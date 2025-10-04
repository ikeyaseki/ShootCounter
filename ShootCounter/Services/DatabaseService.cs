using SQLite;

namespace ShootCounter.Services
{
    public class DatabaseService : IDatabaseService
    {
        private readonly SQLiteAsyncConnection _db;
        public const string DatabaseFilename = "ShootingSQLite.db3";
        public const SQLiteOpenFlags Flags = SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache;

        public static string DatabasePath => Path.Combine(FileSystem.AppDataDirectory, DatabaseFilename);

        private static bool _initDone = false;
        

        public DatabaseService()
        {
            _db = new SQLiteAsyncConnection(DatabasePath, Flags);
        }

        public async Task InitAsync()
        {
            if (_initDone) 
                return;

            await _db.CreateTableAsync<Shooting>();
            await _db.CreateTableAsync<Run>();
            await _db.CreateTableAsync<ShootingCompetition>();

            _initDone = true;
        }

        public async Task AddShootingAsync(Shooting shooting)
        {
            await _db.InsertAsync(shooting);

            if (shooting.Runs != null)
            {
                foreach (var run in shooting.Runs)
                {
                    run.ShootingId = shooting.Id;
                    await _db.InsertAsync(run);
                }
            }
        }

        public async Task<IEnumerable<Shooting>> LoadAllShootingAsync()
        {
            var shootings = await _db.Table<Shooting>().ToArrayAsync();

            foreach (var shooting in shootings)
            {
                shooting.TargetProperties = Targets.TargetProperties.FirstOrDefault(n => n.TargetType == shooting.TargetType);
                shooting.Runs = await _db.Table<Run>().Where(n => n.ShootingId == shooting.Id).ToListAsync();
            }

            return shootings;
        }

        public async Task<Shooting?> LoadShootingAsync(Guid id)
        {
            var shooting = await _db.Table<Shooting>().FirstOrDefaultAsync(n => n.Id == id);

            if (shooting != null)
            {
                shooting.Runs = await _db.Table<Run>().Where(n => n.ShootingId == id).ToListAsync();
            }

            if (shooting != null)
            {
                shooting.TargetProperties = Targets.TargetProperties.FirstOrDefault(n => n.TargetType == shooting.TargetType);
            }

            return shooting;
        }

        public async Task DeleteShootingAsync(Guid id)
        {
            var shooting = await LoadShootingAsync(id);

            if (shooting?.Runs != null)
            {
                await _db.Table<Run>().DeleteAsync(n => n.ShootingId == id);
            }

            await _db.Table<Shooting>().DeleteAsync(n => n.Id == id);
        }

        public async Task UpdateShootingAsync(Shooting shooting)
        {
            if (shooting.Runs != null)
            {
                foreach (var run in shooting.Runs)
                {
                    var existingRun = await _db.Table<Run>().FirstOrDefaultAsync(n => n.Id == run.Id);

                    if (existingRun != null)
                    {
                        await _db.UpdateAsync(run);
                    }
                    else
                    {
                        await _db.InsertAsync(run);
                    }
                }
            }

            await _db.UpdateAsync(shooting);
        }

        public async Task<IEnumerable<ShootingCompetition>> LoadAllShootingCompetitionAsync()
        {
            var shootings = await _db.Table<ShootingCompetition>().ToArrayAsync();

            foreach (var shooting in shootings)
            {
                shooting.CompetitionProperties = Competitions.CompetitionProperties.First(n => n.CompetitionType == shooting.CompetitionType);
                shooting.TargetProperties = Targets.TargetProperties.First(n => n.TargetType == shooting.CompetitionProperties.TargetType);
                shooting.Runs = await _db.Table<Run>().Where(n => n.ShootingId == shooting.Id).ToListAsync();
            }

            return shootings;
        }

        public async Task<ShootingCompetition?> LoadShootingCompetitionAsync(Guid id)
        {
            var shooting = await _db.Table<ShootingCompetition>().FirstOrDefaultAsync(n => n.Id == id);

            if (shooting != null)
            {
                shooting.Runs = await _db.Table<Run>().Where(n => n.ShootingId == id).ToListAsync();
            }

            if (shooting != null)
            {
                shooting.CompetitionProperties = Competitions.CompetitionProperties.First(n => n.CompetitionType == shooting.CompetitionType);
                shooting.TargetProperties = Targets.TargetProperties.First(n => n.TargetType == shooting.CompetitionProperties.TargetType);
            }

            return shooting;
        }

        public async Task AddShootingCompetitionAsync(ShootingCompetition shootingCompetition)
        {
            await _db.InsertAsync(shootingCompetition);

            if (shootingCompetition.Runs != null)
            {
                foreach (var run in shootingCompetition.Runs)
                {
                    run.ShootingId = shootingCompetition.Id;
                    await _db.InsertAsync(run);
                }
            }
        }

        public async Task DeleteShootingCompetitionAsync(Guid id)
        {
            var shooting = await LoadShootingCompetitionAsync(id);

            if (shooting?.Runs != null)
            {
                await _db.Table<Run>().DeleteAsync(n => n.ShootingId == id);
            }

            await _db.Table<ShootingCompetition>().DeleteAsync(n => n.Id == id);
        }

        public async Task UpdateShootingCompetitionAsync(ShootingCompetition shootingCompetition)
        {
            if (shootingCompetition.Runs != null)
            {
                foreach (var run in shootingCompetition.Runs)
                {
                    var existingRun = await _db.Table<Run>().FirstOrDefaultAsync(n => n.Id == run.Id);

                    if (existingRun != null)
                    {
                        await _db.UpdateAsync(run);
                    }
                    else
                    {
                        await _db.InsertAsync(run);
                    }
                }
            }

            await _db.UpdateAsync(shootingCompetition);
        }
    }
}
