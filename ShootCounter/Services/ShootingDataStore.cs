using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShootCounter.Services
{
    public class ShootingDataStore : IDataStore<Shooting>
    {
        private readonly IDatabaseService _db;

        public ShootingDataStore(IDatabaseService db)
        {
            _db = db;
            Task.Run(InitDb);
        }

        private async Task InitDb()
        {
            await _db.InitAsync();
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

        public async Task UpdateItemAsync(Shooting item)
        {
            var existingItem = await GetItemAsync(item.Id);

            if (existingItem != null)
            {
                await _db.UpdateShootingAsync(existingItem);
            }
            else
            {
                await _db.AddShootingAsync(item);
            }
        }
    }
}
