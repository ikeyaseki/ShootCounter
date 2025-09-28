
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
    }
}