using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShootCounter.ViewModels
{
    [QueryProperty(nameof(ShootingId), nameof(ShootingId))]
    public class ShootingDetailViewModel : BaseViewModel
    {
        public Shooting Shooting 
        { 
            get => _shooting; 
            set
            {
                SetProperty(ref _shooting, value);
            }
        }

        private string _shootingId = string.Empty;
        private Shooting _shooting = new();
        private readonly ShootingsViewModel _shootingsViewModel;
        public Command DeleteCommand { get; private set; }

        public string Text { get; set; } = "Shooting details";

        public string ShootingId
        {
            get => _shootingId;
            set
            {
                _shootingId = value;
                LoadShootingsAsync(value);
            }
        }

        public ShootingDetailViewModel(ShootingsViewModel shootingsViewModel)
        {
            _shootingsViewModel = shootingsViewModel;
            Title = "Shooting Detail";
            DeleteCommand = new Command(async () => await ExecuteDelete(), CanExecuteSave);
        }

        private bool CanExecuteSave()
        {
            return Shooting != null;
        }

        private async Task ExecuteDelete()
        {
            bool confirm = await Shell.Current.DisplayAlert(
                "Stop shooting",
                "Are you shure to delete shooting?",
                "Yes",
                "No");

            if (confirm)
            {
                await DataStore.DeleteItemAsync(Shooting.Id);

                await Shell.Current.GoToAsync($"{nameof(ShootingsPage)}", true);
            }
        }

        private async void LoadShootingsAsync(string id)
        {
            try
            {
                IsBusy = true;
                var shooting = await DataStore.GetItemAsync(Guid.Parse(id));
                CalculateShooting calculate = new();
                Shooting = await calculate.Calculate(shooting);
                IsBusy = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            DeleteCommand.ChangeCanExecute();
        }
    }
}
