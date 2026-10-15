
namespace ShootCounter.Services
{
    public class CompetitionDataStore : IDataStore<ShootingCompetition>
    {
        private readonly IDatabaseService _db;

        public CompetitionDataStore(IDatabaseService db)
        {
            _db = db;
        }

        public async Task AddItemAsync(ShootingCompetition item)
        {
            await _db.AddShootingCompetitionAsync(item);
        }

        public async Task DeleteItemAsync(Guid id)
        {
            await _db.DeleteShootingCompetitionAsync(id);
        }

        public async Task<ShootingCompetition?> GetItemAsync(Guid id) => await _db.LoadShootingCompetitionAsync(id);

        public async Task<IEnumerable<ShootingCompetition>> GetItemsAsync(bool forceRefresh = false)
        {
            return await _db.LoadAllShootingCompetitionAsync();
        }

        // inserts the item if it does not exist yet
        public async Task UpdateItemAsync(ShootingCompetition item)
        {
            await _db.UpdateShootingCompetitionAsync(item);
        }
    }
}
