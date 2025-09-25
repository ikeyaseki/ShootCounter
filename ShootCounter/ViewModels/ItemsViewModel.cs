using System.Collections.ObjectModel;
using System.Diagnostics;

namespace ShootCounter.ViewModels;

public class ItemsViewModel : BaseViewModel
{
    private Item? _selectedItem;

    public ObservableCollection<Item> Items { get; private set; } = [];
    public Command LoadItemsCommand { get; }
    public Command ReloadItemsCommand { get; }
    public Command AddItemCommand { get; }
    public Command ClearCommand { get; }
    public Command<Item> ItemTapped { get; }

    public ItemsViewModel()
    {
        Title = "Browse";

        ClearCommand = new Command(() => Items.Clear());
        LoadItemsCommand = new Command(async () => await ExecuteLoadItemsCommand());
        ReloadItemsCommand = new Command(async () => await LoadItemsAsync());
        AddItemCommand = new Command(async () => await OnAddItem());
        ItemTapped = new Command<Item>(OnItemSelected);
    }

    public async Task LoadItemsAsync()
    {
        try
        {
            Items.Clear();

            var dataStoreItems = await DataStore.GetItemsAsync(true);

            foreach (var item in dataStoreItems)
            {
                Items.Add(item);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    async Task ExecuteLoadItemsCommand()
    {
        IsBusy = true;

        await LoadItemsAsync();

        IsBusy = false;
    }

    private async Task OnAddItem()
    {
        await Shell.Current.GoToAsync(nameof(NewItemPage));
    }

    public Item? SelectedItem
    {
        get => _selectedItem;
        set
        {
            SetProperty(ref _selectedItem, value);
            OnItemSelected(value);
        }
    }

    async void OnItemSelected(Item? item)
    {
        if (item == null)
            return;

        // This will push the ItemDetailPage onto the navigation stack
        // Using nameof instead of hard coded string makes it easier to refactor and catch spelling errors
        await Shell.Current.GoToAsync($"{nameof(ItemDetailPage)}?{nameof(ItemDetailViewModel.ItemId)}={item.Id}");
    }
    public void OnAppearing()
    {
        IsBusy = true;
        SelectedItem = null;
    }
}
