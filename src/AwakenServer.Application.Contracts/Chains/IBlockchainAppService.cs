using System.Threading.Tasks;
using AwakenServer.Tokens;

namespace AwakenServer.Chains
{
    public interface IBlockchainAppService
    {
        Task<long> GetBlockNumberAsync(string chainName);
        Task<TokenDto> GetTokenInfoAsync(string chainName, string address, string symbol = null);
    }
}