using System.Threading.Tasks;

namespace AwakenServer.Provider;

public class MockSyncStateProvider : ISyncStateProvider
{
    private long _confirmedBlock;

    public MockSyncStateProvider()
    {
        _confirmedBlock = 2;
    }
    
    public async Task<long> GetLastIrreversibleBlockHeightAsync(string chainId)
    {
        return _confirmedBlock;
    }
}