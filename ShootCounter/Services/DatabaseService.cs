using SQLite;

namespace ShootCounter.Services
{
    public class DatabaseService : IDatabaseService
    {
        private readonly SQLiteAsyncConnection _db;
        public const string DatabaseFilename = "ShootingSQLite.db3";
        public const SQLiteOpenFlags Flags = SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache;

        public static string DatabasePath => Path.Combine(FileSystem.AppDataDirectory, DatabaseFilename);

        private readonly SemaphoreSlim _initLock = new(1, 1);
        private bool _initDone = false;

        public DatabaseService()
        {
            _db = new SQLiteAsyncConnection(DatabasePath, Flags);
        }

        // every public method awaits this, so no query can run before the tables exist
        public async Task InitAsync()
        {
            if (_initDone)
                return;

            await _initLock.WaitAsync();
            try
            {
                if (_initDone)
                    return;

                await _db.CreateTableAsync<Shooting>();
                await _db.CreateTableAsync<Run>();
                await _db.CreateTableAsync<ShootingCompetition>();
                await _db.CreateTableAsync<Weapon>();

                _initDone = true;
            }
            finally
            {
                _initLock.Release();
            }
        }

        public async Task AddShootingAsync(Shooting shooting)
        {
            await InitAsync();

            await _db.RunInTransactionAsync(conn =>
            {
                conn.Insert(shooting);
                SaveRuns(conn, shooting.Id, shooting.Runs);
            });
        }

        public async Task<IEnumerable<Shooting>> LoadAllShootingAsync()
        {
            await InitAsync();

            var shootings = await _db.Table<Shooting>().ToListAsync();

            // load all runs with one query instead of one query per shooting
            var runs = (await _db.QueryAsync<Run>(
                    $"SELECT r.* FROM {nameof(Run)} r INNER JOIN {nameof(Shooting)} s ON r.{nameof(Run.ShootingId)} = s.{nameof(Shooting.Id)} ORDER BY r.{nameof(Run.Id)}"))
                .ToLookup(n => n.ShootingId);

            foreach (var shooting in shootings)
            {
                shooting.TargetProperties = Targets.TargetProperties.FirstOrDefault(n => n.TargetType == shooting.TargetType);
                shooting.Runs = runs[shooting.Id].ToList();
            }

            return shootings;
        }

        public async Task<Shooting?> LoadShootingAsync(Guid id)
        {
            await InitAsync();

            var shooting = await _db.Table<Shooting>().FirstOrDefaultAsync(n => n.Id == id);

            if (shooting != null)
            {
                shooting.Runs = await LoadRunsAsync(id);
                shooting.TargetProperties = Targets.TargetProperties.FirstOrDefault(n => n.TargetType == shooting.TargetType);
            }

            return shooting;
        }

        public async Task DeleteShootingAsync(Guid id)
        {
            await InitAsync();

            await _db.RunInTransactionAsync(conn =>
            {
                conn.Table<Run>().Delete(n => n.ShootingId == id);
                conn.Table<Shooting>().Delete(n => n.Id == id);
            });
        }

        public async Task UpdateShootingAsync(Shooting shooting)
        {
            await InitAsync();

            await _db.RunInTransactionAsync(conn =>
            {
                if (conn.Update(shooting) == 0)
                    conn.Insert(shooting);

                SaveRuns(conn, shooting.Id, shooting.Runs);
            });
        }

        public async Task<IEnumerable<ShootingCompetition>> LoadAllShootingCompetitionAsync()
        {
            await InitAsync();

            var shootings = await _db.Table<ShootingCompetition>().ToListAsync();

            // load all runs with one query instead of one query per competition
            var runs = (await _db.QueryAsync<Run>(
                    $"SELECT r.* FROM {nameof(Run)} r INNER JOIN {nameof(ShootingCompetition)} s ON r.{nameof(Run.ShootingId)} = s.{nameof(ShootingCompetition.Id)} ORDER BY r.{nameof(Run.Id)}"))
                .ToLookup(n => n.ShootingId);

            foreach (var shooting in shootings)
            {
                shooting.CompetitionProperties = Competitions.CompetitionProperties.First(n => n.CompetitionType == shooting.CompetitionType);
                shooting.TargetProperties = Targets.TargetProperties.First(n => n.TargetType == shooting.CompetitionProperties.TargetType);
                shooting.Runs = runs[shooting.Id].ToList();
            }

            return shootings;
        }

        public async Task<ShootingCompetition?> LoadShootingCompetitionAsync(Guid id)
        {
            await InitAsync();

            var shooting = await _db.Table<ShootingCompetition>().FirstOrDefaultAsync(n => n.Id == id);

            if (shooting != null)
            {
                shooting.Runs = await LoadRunsAsync(id);
                shooting.CompetitionProperties = Competitions.CompetitionProperties.First(n => n.CompetitionType == shooting.CompetitionType);
                shooting.TargetProperties = Targets.TargetProperties.First(n => n.TargetType == shooting.CompetitionProperties.TargetType);
            }

            return shooting;
        }

        public async Task AddShootingCompetitionAsync(ShootingCompetition shootingCompetition)
        {
            await InitAsync();

            await _db.RunInTransactionAsync(conn =>
            {
                conn.Insert(shootingCompetition);
                SaveRuns(conn, shootingCompetition.Id, shootingCompetition.Runs);
            });
        }

        public async Task DeleteShootingCompetitionAsync(Guid id)
        {
            await InitAsync();

            await _db.RunInTransactionAsync(conn =>
            {
                conn.Table<Run>().Delete(n => n.ShootingId == id);
                conn.Table<ShootingCompetition>().Delete(n => n.Id == id);
            });
        }

        public async Task UpdateShootingCompetitionAsync(ShootingCompetition shootingCompetition)
        {
            await InitAsync();

            await _db.RunInTransactionAsync(conn =>
            {
                if (conn.Update(shootingCompetition) == 0)
                    conn.Insert(shootingCompetition);

                SaveRuns(conn, shootingCompetition.Id, shootingCompetition.Runs);
            });
        }

        public async Task<IEnumerable<Weapon>> LoadAllWeaponAsync()
        {
            await InitAsync();

            var weapons = await _db.Table<Weapon>().ToListAsync();

            return weapons.OrderBy(n => n.DisplayName, StringComparer.CurrentCultureIgnoreCase).ToList();
        }

        public async Task<Weapon?> LoadWeaponAsync(Guid id)
        {
            await InitAsync();

            return await _db.Table<Weapon>().FirstOrDefaultAsync(n => n.Id == id);
        }

        public async Task SaveWeaponAsync(Weapon weapon)
        {
            await InitAsync();

            // trainings and competitions store the weapon name, a renamed weapon updates it there too
            await _db.RunInTransactionAsync(conn =>
            {
                conn.InsertOrReplace(weapon);
                conn.Execute($"UPDATE {nameof(Shooting)} SET {nameof(Shooting.Weapon)} = ? WHERE {nameof(Shooting.WeaponId)} = ?", weapon.DisplayName, weapon.Id);
                conn.Execute($"UPDATE {nameof(ShootingCompetition)} SET {nameof(ShootingCompetition.Weapon)} = ? WHERE {nameof(ShootingCompetition.WeaponId)} = ?", weapon.DisplayName, weapon.Id);
            });
        }

        // trainings and competitions keep the weapon name as text, so they can still show it
        public async Task DeleteWeaponAsync(Guid id)
        {
            await InitAsync();

            await _db.Table<Weapon>().DeleteAsync(n => n.Id == id);
        }

        private Task<List<Run>> LoadRunsAsync(Guid shootingId) =>
            _db.Table<Run>().Where(n => n.ShootingId == shootingId).OrderBy(n => n.Id).ToListAsync();

        // new runs have Id 0 (auto increment), Insert assigns the generated Id to the object
        private static void SaveRuns(SQLiteConnection conn, Guid shootingId, IEnumerable<Run>? runs)
        {
            if (runs == null)
                return;

            foreach (var run in runs)
            {
                run.ShootingId = shootingId;

                if (run.Id == 0 || conn.Update(run) == 0)
                    conn.Insert(run);
            }
        }
    }
}
