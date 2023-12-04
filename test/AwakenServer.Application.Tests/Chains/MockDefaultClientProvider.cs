using System.Threading.Tasks;
using AElf.Client.MultiToken;
using AwakenServer.Tokens;

namespace AwakenServer.Chains;

public class MockDefaultClientProvider: IBlockchainClientProvider
{
    public string ChainType { get; } = "AElf";
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
    
    public async Task<GetBalanceOutput> GetBalanceAsync(string chainName, string address, string contractAddress,
        string symbol)
    {
        throw new System.NotImplementedException();
    }
}