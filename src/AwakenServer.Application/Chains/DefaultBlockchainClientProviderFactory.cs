using System.Collections.Generic;
using System.Linq;
using Volo.Abp.DependencyInjection;

namespace AwakenServer.Chains
{
    public class DefaultBlockchainClientProviderFactory : IBlockchainClientProviderFactory, ITransientDependency
    {
        private readonly IEnumerable<IBlockchainClientProvider> _blockchainClientProviders;

        public DefaultBlockchainClientProviderFactory(IEnumerable<IBlockchainClientProvider> blockchainClientProviders)
        {
            _blockchainClientProviders = blockchainClientProviders;
        }

        public IBlockchainClientProvider GetBlockChainClientProvider(string chainName)
        {
            switch (chainName)
            {
                case "Ethereum": 
                case "BSC":
                    return _blockchainClientProviders.First(o => o.ChainType == "Ethereum");
                default:
                    return _blockchainClientProviders.First(o => o.ChainType == "AElf");
            }
        }
    }
}