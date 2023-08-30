using System.Collections.Generic;
using System.Linq;
using Volo.Abp.DependencyInjection;

namespace AwakenServer.Chains;

public class MockDefaultBlockchainClientProviderFactory : IBlockchainClientProviderFactory, ITransientDependency
{
    private readonly IEnumerable<IBlockchainClientProvider> _blockchainClientProviders;

    public MockDefaultBlockchainClientProviderFactory(IEnumerable<IBlockchainClientProvider> blockchainClientProviders)
    {
        _blockchainClientProviders = blockchainClientProviders;
    }

    public IBlockchainClientProvider GetBlockChainClientProvider(string chainName)
    {
 
        switch (chainName)
        {
            case "Ethereum":
            case "BSC":
                return _blockchainClientProviders.LastOrDefault(o => o.ChainType == "Ethereum");
            case "AElfMock":
                return _blockchainClientProviders.LastOrDefault(o => o.ChainType == "AElfMock");
            default:
                return _blockchainClientProviders.LastOrDefault(o => o.ChainType == "AElf");
        }
    }
}