namespace ShootCounter.ViewModels
{
    public class NewTrainingViewModel : BaseViewModel
    {
        private string _location = string.Empty;
        private TargetProperties? _selectedTarget = null;
        private int? _bulletsPerRun = null;
        private string _weapon = string.Empty;

        public Command SaveCommand { get; }
        public Command CancelCommand { get; }

        public IEnumerable<TargetProperties> Targets { get; } = Models.Targets.TargetProperties;

        public TargetProperties? SelectedTarget
        {
            get => _selectedTarget;
            set
            {
                SetProperty(ref _selectedTarget, value);
                SaveCommand.ChangeCanExecute();
            }
        }

        public int? BulletsPerRun
        {
            get => _bulletsPerRun;
            set
            {
                SetProperty(ref _bulletsPerRun, value);
                SaveCommand.ChangeCanExecute();
            }
        }

        public List<DistanceType> Distances { get; set; } = new List<DistanceType>() { DistanceType.Distance25m, DistanceType.Distance50m };
        public DistanceType SelectedDistance { get; set; }

        public string Location
        {
            get => _location;
            set
            {
                SetProperty(ref _location, value);
                SaveCommand.ChangeCanExecute();
            }
        }

        public string Weapon 
        { 
            get => _weapon; 
            set
            {
                SetProperty(ref _weapon, value);
                SaveCommand.ChangeCanExecute();
            }
        }

        public NewTrainingViewModel()
        {
            SaveCommand = new Command(OnSave, ValidateSave);
            CancelCommand = new Command(OnCancel);
            Title = "Neues Training";

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

        private async void OnCancel()
        {
            await Shell.Current.GoToAsync("..", true);
        }

        private bool ValidateSave()
        {
            return Location != string.Empty && SelectedTarget != null && BulletsPerRun != null && Weapon != string.Empty;
        }

        private async void OnSave()
        {
            Shooting shooting = new()
            {
                StartTime = DateTime.Now,
                Location = Location,
                TargetType = SelectedTarget?.TargetType ?? TargetType.Unknown,
                Weapon = Weapon,
                DistanceType = SelectedDistance,
                BulletsPerRun = BulletsPerRun
            };

            await TrainingDataStore.AddItemAsync(shooting);

            await Shell.Current.GoToAsync($"{nameof(RunPage)}?{nameof(RunViewModel.ShootingId)}={shooting.Id.ToString()}", true);
        }
    }
}
