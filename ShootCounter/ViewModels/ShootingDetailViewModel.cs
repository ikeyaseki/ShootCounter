using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShootCounter.ViewModels
{
    [QueryProperty(nameof(ShootingId), nameof(ShootingId))]
    public class ShootingDetailViewModel : BaseViewModel
    {
        private Shooting _shooting = new();
        private string _shootingId = string.Empty;
        private readonly ShootingsViewModel _shootingsViewModel;
        public Command SaveCommand { get; private set; }

        public string Text { get; set; } = "blubbbb";

        public string ShootingId 
        { 
            get => _shootingId;
            set
            {
                _shootingId = value;

                Title = $"Detail {_shootingId}";
                SetProperty(ref _shootingId, value);
            }
        }

        public ShootingDetailViewModel(ShootingsViewModel shootingsViewModel)
        {
            _shootingsViewModel = shootingsViewModel;
            SaveCommand = new Command(async () => await ExecuteSave(), CanExecuteSave);
        }

        private bool CanExecuteSave()
        {
            return true;
        }

        private Task ExecuteSave()
        {
            return Task.CompletedTask;
        }
    }
}
