using System.Diagnostics;

namespace ShootCounter.ViewModels
{
    [QueryProperty(nameof(ItemId), nameof(ItemId))]
    public class ItemDetailViewModel : BaseViewModel
    {
        private Item item = new();
        private readonly ItemsViewModel ItemsViewModel;
        public Command SaveCommand { get; private set; }

        private string text = string.Empty;
        public string Text
        {
            get => text;
            set
            {
                if (SetProperty(ref text, value))
                {
                    if (value != item.Text)
                        Title = $"Modified {item.Text}";
                    else
                        Title = $"{item.Text}";
                }
            }
        }

        private string? description;
        public string? Description
        {
            get => description;
            set => SetProperty(ref description, value);
        }

        private string itemId = string.Empty;
        public string ItemId
        {
            get => itemId;
            set
            {
                itemId = value;
                LoadItemId(value);
            }
        }

        public async void LoadItemId(string itemId)
        {
            try
            {
                item = await DataStore.GetItemAsync(itemId);
                Text = item.Text;
                Description = item.Description;
            }
            catch (Exception)
            {
                Debug.WriteLine("Failed to Load Item");
            }
        }

        public ItemDetailViewModel(ItemsViewModel ItemsViewModel)
        {
            this.ItemsViewModel = ItemsViewModel;
            SaveCommand = new Command(async () => await ExecuteSave(), CanExecuteSave);
            this.PropertyChanged += (_, __) => SaveCommand.ChangeCanExecute();
        }

        private bool CanExecuteSave()
        {
            if (item != null)
                return item.Description != Description || item.Text != Text;
            return false;
        }

        private async Task ExecuteSave()
        {
            item.Description = Description ?? string.Empty;
            item.Text = Text;

            _ = await DataStore.UpdateItemAsync(item);
            ItemsViewModel.ReloadItemsCommand.Execute(null);

            await Shell.Current.GoToAsync("..");
        }
    }
}
