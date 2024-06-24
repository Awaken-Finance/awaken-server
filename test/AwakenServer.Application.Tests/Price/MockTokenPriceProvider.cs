using System;
using System.Threading.Tasks;

namespace AwakenServer.Price;

public class MockTokenPriceProvider : ITokenPriceProvider
{
    private int _usdtCallCount = 0;

    public async Task<decimal> GetPriceAsync(string pair)
    {
        switch (pair)
        {
            case "eth-usd":
                return 1.2m;
            case "usdt-usd":
                return 1.1m;
            case "usdc-usd":
                return 1m;
            case "sgr-usdt":
                return 2m;
            case "testcache-usdt":
                ++_usdtCallCount;
                if (_usdtCallCount == 1)
                {
                    return 1.3m;
                }
                else if (_usdtCallCount == 2)
                {
                    throw new Exception("Test price cache");
                }
                else
                {
                    return 3.3m;
                }
            default:
                return 123;
        }
    }

    public async Task<decimal> GetHistoryPriceAsync(string pair, string dateTime)
    {
        return 0.0m;
    }
}