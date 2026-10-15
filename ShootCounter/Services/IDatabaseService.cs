
namespace ShootCounter.Services
{
    public interface IDatabaseService
    {
        Task InitAsync();
        Task<IEnumerable<Shooting>> LoadAllShootingAsync();
        Task<Shooting?> LoadShootingAsync(Guid id);
        Task AddShootingAsync(Shooting shooting);
        Task DeleteShootingAsync(Guid id);
        Task UpdateShootingAsync(Shooting shooting);

        Task<IEnumerable<ShootingCompetition>> LoadAllShootingCompetitionAsync();
        Task<ShootingCompetition?> LoadShootingCompetitionAsync(Guid id);
        Task AddShootingCompetitionAsync(ShootingCompetition shootingCompetition);
        Task DeleteShootingCompetitionAsync(Guid id);
        Task UpdateShootingCompetitionAsync(ShootingCompetition shootingCompetition);

        Task<IEnumerable<Weapon>> LoadAllWeaponAsync();
        Task<Weapon?> LoadWeaponAsync(Guid id);
        Task SaveWeaponAsync(Weapon weapon);
        Task DeleteWeaponAsync(Guid id);
        Task<(int Trainings, int Competitions)> CountWeaponUsageAsync(Guid id);
    }
}