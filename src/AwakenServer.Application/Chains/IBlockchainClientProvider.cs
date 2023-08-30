using System.Threading.Tasks;
using AwakenServer.Tokens;

namespace AwakenServer.Chains
{
    public interface IBlockchainClientProvider
    {
        string ChainType { get; }
        Task<long> GetBlockNumberAsync(string chainName);
        Task<TokenDto> GetTokenInfoAsync(string chainName, string address, string symbol);
    }
}