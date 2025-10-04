using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShootCounter.ViewModels
{
    public class TrainingsViewModel : BaseViewModel
    {
        private Shooting? _selectedShoting;

        public ObservableCollection<Shooting> Shootings { get; private set; } = [];
        public Command LoadShootingsCommand { get; }
        public Command AddShootingCommand { get; }
        public Command<Shooting> ShootingTapped { get; }
        public Command SelectionChangedCommand { get; }
        public Command DetailsCommand { get; }
        public Command ReloadShootingsCommand { get; }

        public TrainingsViewModel()
        {
            Title = "Trainings";
            LoadShootingsCommand = new Command(async () => await ExecuteLoadShootingsCommand());
            AddShootingCommand = new Command(async () => await ExecuteAddShootingCommand());
            ShootingTapped = new Command<Shooting>(OnShootingSelected);
            SelectionChangedCommand = new Command<Shooting>(OnSelectionChangedCommand);
            DetailsCommand = new Command(async () => await ExecuteDetailsCommand(), CanExecuteDetailsCommand);
            ReloadShootingsCommand = new Command(async () => await LoadShootingsAsync());

            Shell.Current.Navigating += (s, e) =>
            {
                if (e.Source == ShellNavigationSource.Pop && e.CanCancel)
                {
                    if (Shell.Current.CurrentPage is RunPage)
                    {
                        e.Cancel();
                    }
                }
            };
        }

        private bool CanExecuteDetailsCommand()
        {
            return SelectedShooting != null;
        }

        private Task ExecuteDetailsCommand()
        {
            OnShootingSelected(SelectedShooting);

            return Task.CompletedTask;
        }

        private void OnSelectionChangedCommand(Shooting shooting)
        {
            SelectedShooting = shooting;
        }

        private async Task ExecuteAddShootingCommand()
        {
            await Shell.Current.GoToAsync(nameof(NewTrainingPage), true);
        }

        private async Task ExecuteLoadShootingsCommand()
        {
            IsBusy = true;

            await LoadShootingsAsync();

            IsBusy = false;
        }

        private async Task LoadShootingsAsync()
        {
            try
            {
                Shootings.Clear();

                var shootingStoreItems = await DataStore.GetItemsAsync(true);

                foreach (var shooting in shootingStoreItems)
                {
                    Shootings.Add(await CalculateResults(shooting));
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private async Task<Shooting> CalculateResults(Shooting shooting)
        {
            CalculateShooting calculate = new();

            shooting = await calculate.Calculate(shooting);

            return shooting;
        }

        public Shooting? SelectedShooting
        {
            get => _selectedShoting;
            set
            {
                SetProperty(ref _selectedShoting, value);
                DetailsCommand.ChangeCanExecute();
            }
        }

        async void OnShootingSelected(Shooting? shooting)
        {
            if (shooting == null)
                return;

            await Shell.Current.GoToAsync($"{nameof(TrainingDetailPage)}?{nameof(TrainingDetailViewModel.ShootingId)}={shooting.Id.ToString()}", true);
        }

        public void OnAppearing()
        {
            IsBusy = true;
            SelectedShooting = null;
        }
    }
}
