using System.Threading.Tasks;
using AElf.Client.MultiToken;
using AwakenServer.Chains;
using AwakenServer.Tokens;
using TokenInfo = AElf.Contracts.MultiToken.TokenInfo;

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
        switch (symbol)
        {
            case "NewToken":
                return Task.FromResult(new TokenDto
                {
                    Address = "7RzVGiuVWkvL4VfVHdZfQF2Tri3sgLe9U991bohHFfSRZXuGX",
                    Decimals = 8,
                    Symbol = symbol,
                    ImageUri = "TestImageUri"
                });
            default:
                return Task.FromResult<TokenDto>(new TokenDto()
                {
                    Address = "0x123456789",
                    Decimals = 8,
                    Symbol = symbol
                }); 
        }
        
    }

    public async Task<int> ExistTransactionAsync(string chainName, string transactionHash)
    {
        if (chainName == "AELF") return 1;
        else if (chainName == "tDVV") return 0;
        else return -1;
    }

    public async Task<TokenInfo> GetTokenInfoFromChainAsync(string chainName, string address, string symbol)
    {
        return new TokenInfo()
        {
            Symbol = symbol,
            Decimals = 8
        };
    }


    public async Task<GetBalanceOutput> GetBalanceAsync(string chainName, string address, string contractAddress,
        string symbol)
    {
        if (symbol == "no")
        {
            return new GetBalanceOutput()
            {
                Balance = 1,
                Symbol = "no",
            };
        }

        return new GetBalanceOutput()
        {
            Balance = 1,
            Symbol = symbol,
        };
    }
}