using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShootCounter.ViewModels
{
    [QueryProperty(nameof(ShootingCompetitionId), nameof(ShootingCompetitionId))]
    public class CompetitionDetailViewModel : BaseViewModel
    {
        private string _shootingCompetitionId;

        public string ShootingCompetitionId 
        { 
            get => _shootingCompetitionId; 
            set
            {
                SetProperty(ref _shootingCompetitionId, value);
            }
        }

        public CompetitionDetailViewModel()
        {
            Title = "Competition detail";
        }
    }
}
