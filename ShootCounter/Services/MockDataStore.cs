namespace ShootCounter.Services
{
    public class MockDataStore : IDataStore<Item>
    {
        readonly List<Item> items;

        public MockDataStore()
        {
            items =
                [
                    new Item { Id = Guid.NewGuid().ToString(), Text = "First item", Description = "This is an item description" },
                    new Item { Id = Guid.NewGuid().ToString(), Text = "Second item", Description = "This is an item description" },
                    new Item { Id = Guid.NewGuid().ToString(), Text = "Third item", Description = "This is an item description" },
                    new Item { Id = Guid.NewGuid().ToString(), Text = "Fourth item", Description = "This is an item description" },
                    new Item { Id = Guid.NewGuid().ToString(), Text = "Fifth item", Description = "This is an item description" },
                ];
        }

        public async Task<bool> AddItemAsync(Item item)
        {
            items.Add(item);

            return await Task.FromResult(true);
        }

        public async Task<bool> DeleteItemAsync(string id)
        {
            var existingItem = await GetItemAsync(id);

            if (existingItem != null)
            {
                items.Remove(existingItem);
            }

            return await Task.FromResult(true);
        }

        public async Task<Item> GetItemAsync(string id) => await Task.FromResult(items.First(x => x.Id == id));

        public async Task<IEnumerable<Item>> GetItemsAsync(bool forceRefresh = false)
        {
            return await Task.FromResult(items);
        }

        public async Task<bool> UpdateItemAsync(Item item)
        {
            var existingItem = await GetItemAsync(item.Id);

            if (existingItem != null)
            {
                existingItem.Text = item.Text;
                existingItem.Description = item.Description;
            }
            else
            {
                items.Add(item);
            }

            return await Task.FromResult(true);
        }
    }
}
