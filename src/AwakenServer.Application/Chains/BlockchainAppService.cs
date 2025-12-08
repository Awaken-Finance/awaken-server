using System.Threading.Tasks;
using AwakenServer.Tokens;

namespace AwakenServer.Chains
{
    public class BlockchainAppService : AwakenServerAppService, IBlockchainAppService
    {
        private readonly IBlockchainClientProviderFactory _blockchainClientProviderFactory;

        public BlockchainAppService(IBlockchainClientProviderFactory blockchainClientProviderFactory)
        {
            _blockchainClientProviderFactory = blockchainClientProviderFactory;
        }

        public async Task<long> GetBlockNumberAsync(string chainName)
        {
            var provider = _blockchainClientProviderFactory.GetBlockChainClientProvider(chainName);
            return await provider.GetBlockNumberAsync(chainName);
        }

        public async Task<TokenDto> GetTokenInfoAsync(string chainName, string address, string symbol)
        {
            var provider = _blockchainClientProviderFactory.GetBlockChainClientProvider(chainName);
            return await provider.GetTokenInfoAsync(chainName, address, symbol);
        }
    }
}