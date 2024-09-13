using System.Threading.Tasks;

namespace AwakenServer.Provider;

public interface ISyncStateProvider
{
    public Task<long> GetLastIrreversibleBlockHeightAsync(string chainId);
}