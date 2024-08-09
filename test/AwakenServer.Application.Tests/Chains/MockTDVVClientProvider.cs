using System.Threading.Tasks;
using AElf.Client.MultiToken;
using AwakenServer.Tokens;

namespace AwakenServer.Chains;

public class MockTDVVClientProvider: IBlockchainClientProvider
{
    public string ChainType { get; } = "tDVVMock";
    public async Task<long> GetBlockNumberAsync(string chainName)
    {
        return 1000L;
    }

    public  Task<TokenDto> GetTokenInfoAsync(string chainName, string address, string symbol)
    {
        switch (symbol)
        {
            case "BTC":
                return Task.FromResult(new TokenDto
                {
                    Address = "7RzVGiuVWkvL4VfVHdZfQF2Tri3sgLe9U991bohHFfSRZXuGX",
                    Decimals = 8,
                    Symbol = symbol
                });
            case "ETH":
                return Task.FromResult(new TokenDto
                {
                    Address = "7RzVGiuVWkvL4VfVHdZfQF2Tri3sgLe9U991bohHFfSRZXuGX",
                    Decimals = 8,
                    Symbol = symbol
                });
            case "ELF":
                return Task.FromResult(new TokenDto
                {
                    Address = "7RzVGiuVWkvL4VfVHdZfQF2Tri3sgLe9U991bohHFfSRZXuGX",
                    Decimals = 8,
                    Symbol = symbol
                });
            case "USDT":
                return Task.FromResult(new TokenDto
                {
                    Address = "7RzVGiuVWkvL4VfVHdZfQF2Tri3sgLe9U991bohHFfSRZXuGX",
                    Decimals = 8,
                    Symbol = symbol
                });
            case "NewToken":
                return Task.FromResult(new TokenDto
                {
                    Address = "7RzVGiuVWkvL4VfVHdZfQF2Tri3sgLe9U991bohHFfSRZXuGX",
                    Decimals = 8,
                    Symbol = symbol,
                    ImageUri = "TestImageUri"
                });
            default:
                return Task.FromResult(new TokenDto
                {
                    Address = "7RzVGiuVWkvL4VfVHdZfQF2Tri3sgLe9U991bohHFfSRZXuGX",
                    Decimals = 8,
                    Symbol = symbol,
                });
                
        }
        
    }
    
    public async Task<GetBalanceOutput> GetBalanceAsync(string chainName, string address, string contractAddress,
        string symbol)
    {
        throw new System.NotImplementedException();
    }
}