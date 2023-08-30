using System.Threading.Tasks;
using AwakenServer.Tokens;

namespace AwakenServer.Chains;

public class MockAElfClientProvider : IBlockchainClientProvider
{
    public string ChainType { get; } = "AElfMock";
    public async Task<long> GetBlockNumberAsync(string chainName)
    {
        return 1000L;
    }

    public  Task<TokenDto> GetTokenInfoAsync(string chainName, string address, string symbol)
    {
        return Task.FromResult(new TokenDto
        {
            Address = address,
            Decimals = 18,
            Symbol = symbol
        });
    }
}