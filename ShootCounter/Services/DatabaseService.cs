using SQLite;

namespace ShootCounter.Services
{
    public class DatabaseService : IDatabaseService
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        private static SQLiteAsyncConnection _db;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public const string DatabaseFilename = "ShootingSQLite.db3";
        public const SQLiteOpenFlags Flags = SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache;

        public static string DatabasePath => Path.Combine(FileSystem.AppDataDirectory, DatabaseFilename);

        public static readonly AsyncLazy<DatabaseService> Instance = new(async () =>
        {
            var instance = new DatabaseService();

#pragma warning disable CS8602 // Dereference of a possibly null reference.
            _ = await _db.CreateTableAsync<Shooting>();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            _ = await _db.CreateTableAsync<Run>();
            _ = await _db.CreateTableAsync<TargetProperties>();

            return instance;
        });

        public DatabaseService()
        {
            _db = new SQLiteAsyncConnection(DatabasePath, Flags);
        }

        public async Task InitAsync()
        {
            await _db.CreateTableAsync<Shooting>();
            await _db.CreateTableAsync<Run>();
            await _db.CreateTableAsync<TargetProperties>();
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

        public async Task AddTargetPropertiesAsync(TargetProperties targetProperties)
        {
            await _db.InsertAsync(targetProperties);
        }


        public async Task<IEnumerable<Shooting>> LoadAllShootingAsync()
        {
            return await _db.Table<Shooting>().ToArrayAsync();
        }

        public async Task<Shooting?> LoadShootingAsync(Guid id)
        {
            var shooting = await _db.Table<Shooting>().FirstOrDefaultAsync(s => s.Id == id);

            if (shooting != null)
            {
                shooting.Runs = await _db.Table<Run>().Where(r => r.ShootingId == id).ToListAsync();
            }

            if (shooting != null)
            {
                shooting.TargetProperties = await _db.Table<TargetProperties>().FirstAsync(r => r.TargetType == shooting.TargetType);
            }

            return shooting;
        }

        public async Task<IEnumerable<TargetProperties>> LoadAllTargetPropertiesAsync()
        {
            return await _db.Table<TargetProperties>().ToArrayAsync();
        }

        public async Task<TargetProperties> LoadTargetProperties(TargetType targetType)
        {
            return await _db.Table<TargetProperties>().FirstAsync(n => n.TargetType == targetType);
        }

        public async Task DeleteShootingAsync(Guid id)
        {
            await _db.Table<Shooting>().DeleteAsync(n => n.Id == id);
        }

        public async Task UpdateShootingAsync(Shooting shooting)
        {
            if (shooting.Runs != null)
            {
                foreach (var run in shooting.Runs)
                {
                    await _db.UpdateAsync(run);
                }
            }

            await _db.UpdateAsync(shooting);
        }
    }
}
