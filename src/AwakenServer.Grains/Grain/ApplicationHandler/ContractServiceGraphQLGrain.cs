using System.Threading.Tasks;
using AwakenServer.Grains.State.ApplicationHandler;
using Orleans;

namespace AwakenServer.Grains.Grain.ApplicationHandler;

public class ContractServiceGraphQLGrain : Grain<GraphQlState>, IContractServiceGraphQLGrain
{
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }

    public async Task SetStateAsync(long height)
    {
        State.EndHeight = height;
        await WriteStateAsync();
    }

    public Task<long> GetStateAsync()
    {
        return Task.FromResult(State.EndHeight);
    }
}