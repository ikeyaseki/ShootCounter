namespace ShootCounter.ViewModels
{
    public class NewItemViewModel : BaseViewModel
    {
        private readonly ItemsViewModel ItemsViewModel;
        private string text = string.Empty;
        public string Text
        {
            get => text;
            set => SetProperty(ref text, value);
        }

        private string description = string.Empty;
        public string Description
        {
            get => description;
            set => SetProperty(ref description, value);
        }

        public Command SaveCommand { get; }
        public Command CancelCommand { get; }

        public NewItemViewModel(ItemsViewModel ItemsViewModel)
        {
            this.ItemsViewModel = ItemsViewModel;
            SaveCommand = new Command(OnSave, ValidateSave);
            CancelCommand = new Command(OnCancel);

            this.PropertyChanged += (_, __) => SaveCommand.ChangeCanExecute();
        }

        private bool ValidateSave()
        {
            return !String.IsNullOrWhiteSpace(text)
                && !String.IsNullOrWhiteSpace(description);
        }

        private async void OnSave()
        {
            Item newItem = new()
            {
                Id = Guid.NewGuid().ToString(),
                Text = Text,
                Description = Description
            };

            await DataStore.AddItemAsync(newItem);

            ItemsViewModel.ReloadItemsCommand.Execute(null);

            // This will pop the current page off the navigation stack
            await Shell.Current.GoToAsync("..");
        }

        private async void OnCancel()
        {
            // This will pop the current page off the navigation stack
            await Shell.Current.GoToAsync("..");
        }
    }
}
