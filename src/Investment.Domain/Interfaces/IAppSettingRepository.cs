using Investment.Domain.Entities;

namespace Investment.Domain.Interfaces;

public interface IAppSettingRepository
{
    Task<AppSetting?> GetAsync(string key);
    Task SetAsync(string key, string value);
    Task<IEnumerable<AppSetting>> GetAllAsync();
}
