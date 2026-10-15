
namespace ShootCounter.Services
{
    public class WeaponDataStore : IDataStore<Weapon>
    {
        private readonly IDatabaseService _db;

        public WeaponDataStore(IDatabaseService db)
        {
            _db = db;
        }

        public async Task AddItemAsync(Weapon item)
        {
            await _db.SaveWeaponAsync(item);
        }

        public async Task DeleteItemAsync(Guid id)
        {
            await _db.DeleteWeaponAsync(id);
        }

        public async Task<Weapon?> GetItemAsync(Guid id) => await _db.LoadWeaponAsync(id);

        public async Task<IEnumerable<Weapon>> GetItemsAsync(bool forceRefresh = false)
        {
            return await _db.LoadAllWeaponAsync();
        }

        // inserts the item if it does not exist yet
        public async Task UpdateItemAsync(Weapon item)
        {
            await _db.SaveWeaponAsync(item);
        }
    }
}
