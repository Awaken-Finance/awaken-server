using Orleans;

namespace AwakenServer.Grains.Grain.Trade;

public interface ILiquiditySyncGrain : IGrainWithStringKey
{
    public Task AddTransactionHashAsync(string transactionHash);

    public Task<bool> ExistTransactionHashAsync(string transactionHash);
}