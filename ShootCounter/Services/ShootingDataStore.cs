using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShootCounter.Services
{
    public class ShootingDataStore : IItemStore<Shooting>
    {
        readonly List<Shooting> _items;

        public ShootingDataStore()
        {
            _items =
                [
                    new Shooting { DistanceType = DistanceType.Distance25m, StartTime = DateTime.Now.AddHours(-5), EndTime = DateTime.Now.AddHours(-4), TargetType = TargetType.OrderlyRapidFire, Location = "Teufmoos", Weapon = "CZ TS2" },
                    new Shooting { DistanceType = DistanceType.Distance25m, StartTime = DateTime.Now.AddHours(-4), EndTime = DateTime.Now.AddHours(-3), TargetType = TargetType.OrderlyRapidFire, Location = "Teufmoos", Weapon = "P220" },
                    new Shooting { DistanceType = DistanceType.Distance25m, StartTime = DateTime.Now.AddHours(-3), EndTime = DateTime.Now.AddHours(-2), TargetType = TargetType.OrderlyRapidFire, Location = "Teufmoos", Weapon = ".357" },
                ];
        }

        public Task<bool> AddItemAsync(Shooting item)
        {
            _items.Add(item);

            return Task.FromResult(true);
        }

        public async Task<bool> DeleteItemAsync(Guid id)
        {
            var existingItem = await GetItemAsync(id);

            if (existingItem != null)
            {
                _items.Remove(existingItem);
            }

            return await Task.FromResult(true);
        }

        public async Task<Shooting> GetItemAsync(Guid id) => await Task.FromResult(_items.First(x => x.Id == id));

        public async Task<IEnumerable<Shooting>> GetItemsAsync(bool forceRefresh = false)
        {
            return await Task.FromResult(_items);
        }

        public async Task<bool> UpdateItemAsync(Shooting item)
        {
            var existingItem = await GetItemAsync(item.Id);

            if (existingItem != null)
            {
                existingItem.Location = item.Location;
                existingItem.Weapon = item.Weapon;  
                existingItem.StartTime = item.StartTime;
                existingItem.EndTime = item.EndTime;
                existingItem.TargetType = item.TargetType;
            }
            else
            {
                _items.Add(item);
            }

            return await Task.FromResult(true);
        }
    }
}
