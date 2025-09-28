
namespace ShootCounter.Services
{
    public interface IDatabaseService
    {
        Task InitAsync();
        Task<IEnumerable<Shooting>> LoadAllShootingAsync();
        Task<IEnumerable<TargetProperties>> LoadAllTargetPropertiesAsync();
        Task<Shooting?> LoadShootingAsync(Guid id);
        Task<TargetProperties> LoadTargetProperties(TargetType targetType);
        Task AddShootingAsync(Shooting shooting);
        Task AddTargetPropertiesAsync(TargetProperties targetProperties);
        Task DeleteShootingAsync(Guid id);
        Task UpdateShootingAsync(Shooting shooting);
    }
}