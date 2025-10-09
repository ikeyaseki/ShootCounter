using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShootCounter.Services
{
    public class NavigationService : INavigationService
    {    
        public void HandleNavigationEvent()
        {
            Shell.Current.Navigating += (s, e) =>
            {
                if (e.Source == ShellNavigationSource.Pop && e.CanCancel)
                {
                    switch (Shell.Current.CurrentPage.GetType().Name)
                    {
                        case nameof(AboutPage):
                        case nameof(CompetitionRunPage):
                        case nameof(CompetitionsPage):
                        case nameof(NewCompetitionPage):
                        case nameof(NewTrainingPage):
                        case nameof(RunPage):
                        case nameof(TrainingsPage):
                            e.Cancel();
                            break;
                    }
                }
            };
        }
    }
}
