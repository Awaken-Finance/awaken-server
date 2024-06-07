using System.Threading.Tasks;

namespace AwakenServer.Grains.Grain.Tokens.TokenPrice;

public interface ITokenPriceProvider
{
    Task<decimal> GetPriceAsync(string symbol);
    Task<decimal> GetHistoryPriceAsync(string symbol, string dateTime);
}