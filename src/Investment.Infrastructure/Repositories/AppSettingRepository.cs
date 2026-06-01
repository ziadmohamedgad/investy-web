using Investment.Domain.Entities;
using Investment.Domain.Interfaces;
using Investment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Investment.Infrastructure.Repositories;

public class AppSettingRepository : IAppSettingRepository
{
    private readonly InvestmentDbContext _context;

    public AppSettingRepository(InvestmentDbContext context)
    {
        _context = context;
    }

    public async Task<AppSetting?> GetAsync(string key)
    {
        return await _context.AppSettings.FindAsync(key);
    }

    public async Task SetAsync(string key, string value)
    {
        var setting = await _context.AppSettings.FindAsync(key);
        if (setting != null)
        {
            setting.SettingValue = value;
            setting.LastUpdated = DateTime.UtcNow;
            _context.AppSettings.Update(setting);
        }
        else
        {
            _context.AppSettings.Add(new AppSetting
            {
                SettingKey = key,
                SettingValue = value,
                LastUpdated = DateTime.UtcNow
            });
        }
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<AppSetting>> GetAllAsync()
    {
        return await _context.AppSettings.ToListAsync();
    }
}
