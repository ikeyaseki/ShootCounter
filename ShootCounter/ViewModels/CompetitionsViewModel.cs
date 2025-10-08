
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace ShootCounter.ViewModels
{
    public class CompetitionsViewModel : BaseViewModel
    {
        private ShootingCompetition? _selectedShootingCompetition;

        public ObservableCollection<ShootingCompetition> ShootingCompetitions { get; private set; } = [];
        public Command LoadShootingsCommand { get; }
        public Command AddShootingCommand { get; }
        public Command<ShootingCompetition> ShootingTapped { get; }
        public Command SelectionChangedCommand { get; }
        public Command DetailsCommand { get; }
        public Command ReloadShootingsCommand { get; }

        public ShootingCompetition? SelectedShootingCompetition 
        { 
            get => _selectedShootingCompetition; 
            set
            {
                SetProperty(ref _selectedShootingCompetition, value);
                DetailsCommand.ChangeCanExecute();
            }
        }

        public CompetitionsViewModel()
        {
            Title = "Wettkämpfe";
            LoadShootingsCommand = new Command(async () => await ExecuteLoadShootingsCommand());
            AddShootingCommand = new Command(async () => await ExecuteAddShootingCommand());
            ShootingTapped = new Command<ShootingCompetition>(OnShootingSelected);
            SelectionChangedCommand = new Command<ShootingCompetition>(OnSelectionChangedCommand);
            DetailsCommand = new Command(async () => await ExecuteDetailsCommand(), CanExecuteDetailsCommand);
            ReloadShootingsCommand = new Command(async () => await LoadShootingsAsync());

            Shell.Current.Navigating += (s, e) =>
            {
                if (e.Source == ShellNavigationSource.Pop && e.CanCancel)
                {
                    e.Cancel();
                }
            };
        }

        private async Task LoadShootingsAsync()
        {
            try
            {
                ShootingCompetitions.Clear();

                var shootingStoreItems = await CompetitionDataStore.GetItemsAsync(true);

                foreach (var shooting in shootingStoreItems)
                {
                    ShootingCompetitions.Add(await CalculateResults(shooting));
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private bool CanExecuteDetailsCommand()
        {
            return SelectedShootingCompetition != null;
        }

        private Task ExecuteDetailsCommand()
        {
            OnShootingSelected(SelectedShootingCompetition);

            return Task.CompletedTask;
        }

        private void OnSelectionChangedCommand(ShootingCompetition shooting)
        {
            SelectedShootingCompetition = shooting;
        }

        async void OnShootingSelected(ShootingCompetition? shooting)
        {
            if (shooting == null)
                return;

            await Shell.Current.GoToAsync($"{nameof(CompetitionDetailPage)}?{nameof(CompetitionDetailViewModel.ShootingCompetitionId)}={shooting.Id.ToString()}", true);
        }

        private async Task ExecuteAddShootingCommand()
        {
            await Shell.Current.GoToAsync(nameof(NewCompetitionPage), true);
        }

        private async Task ExecuteLoadShootingsCommand()
        {
            IsBusy = true;

            await LoadShootingsAsync();

            IsBusy = false;
        }

        private async Task<ShootingCompetition> CalculateResults(ShootingCompetition shooting)
        {
            CalculateShooting calculate = new();

            shooting = await calculate.CalculateCompetition(shooting);

            return shooting;
        }

        public void OnAppearing()
        {
            IsBusy = true;
            SelectedShootingCompetition = null;
        }
    }
}
