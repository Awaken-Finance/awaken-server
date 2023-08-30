using System.Threading.Tasks;
using AwakenServer.Chains;
using AwakenServer.Tokens;

namespace AwakenServer.Provider;

public class MockAelfClientProvider : IAElfClientProvider
{
    public async Task<long> GetTransactionFeeAsync(string chainName, string transactionId)
    {
        return 1;
    }

    public string ChainType { get; } = "AElf";
    public Task<long> GetBlockNumberAsync(string chainName)
    {
        throw new System.NotImplementedException();
    }

    public Task<TokenDto> GetTokenInfoAsync(string chainName, string address, string symbol)
    {
        return Task.FromResult<TokenDto>(new TokenDto()
        {
            Address = "0x123456789",
            Decimals = 8
        });
    }

    public async Task<int> ExistTransactionAsync(string chainName, string transactionHash)
    {
        if (chainName == "AELF") return 1;
        else if (chainName == "tDVV") return 0;
        else return -1;
    }
}