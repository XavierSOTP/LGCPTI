using LGC.PIT.Plugin.Common.Database.Entities;
using System.Threading.Tasks;

namespace LGC.PIT.Plugin.Common.Database.Repositories;

public interface IConfigurationRepository : IRepository<int, Configuration>
{
    Task<Configuration> GetByKeyAsync(string key);
    public Task UpsertAsync(string key, string value);
}
