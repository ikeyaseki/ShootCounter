
namespace ShootCounter.Services
{
    public class TrainingDataStore : IDataStore<Shooting>
    {
        private readonly IDatabaseService _db;

        public TrainingDataStore(IDatabaseService db)
        {
            _db = db;
        }

        public async Task AddItemAsync(Shooting item)
        {
            await _db.AddShootingAsync(item);
        }

        public async Task DeleteItemAsync(Guid id)
        {
            await _db.DeleteShootingAsync(id);
        }

        public async Task<Shooting?> GetItemAsync(Guid id) => await _db.LoadShootingAsync(id);

        public async Task<IEnumerable<Shooting>> GetItemsAsync(bool forceRefresh = false)
        {
            return await _db.LoadAllShootingAsync();
        }

        // inserts the item if it does not exist yet
        public async Task UpdateItemAsync(Shooting item)
        {
            await _db.UpdateShootingAsync(item);
        }
    }
}
