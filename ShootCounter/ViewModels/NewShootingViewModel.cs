using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ShootCounter.ViewModels
{
    public class NewShootingViewModel : BaseViewModel
    {
        private readonly ShootingsViewModel _shootingsViewModel;
        private Shooting _shooting = new Shooting();

        public Command SaveCommand { get; }
        public Command CancelCommand { get; }

        public Targets Targets { get; } = new Targets();

        public TargetProperties? SelectedTarget { get; set; } = null;

        public List<DistanceType> Distances { get; set; } = new List<DistanceType>() { DistanceType.Distance25m, DistanceType.Distance50m };
        public DistanceType SelectedDistance { get; set; }

        public Shooting Shooting 
        { 
            get => _shooting;
            set => SetProperty(ref _shooting, value); 
        }
        public NewShootingViewModel(ShootingsViewModel shootingsViewModel)
        {
            _shootingsViewModel = shootingsViewModel;
            SaveCommand = new Command(OnSave, ValidateSave);
            CancelCommand = new Command(OnCancel);
            Title = "Add new shooting";
        }

        private async void OnCancel()
        {
            await Shell.Current.GoToAsync("..");
        }

        private bool ValidateSave()
        {
            return true;
        }

        private async void OnSave()
        {
            Shooting.StartTime = DateTime.Now;

            await ShootingStore.AddItemAsync(Shooting);

            _shootingsViewModel.ReloadShootingsCommand.Execute(null);

            await Shell.Current.GoToAsync("..");
        }
    }
}
