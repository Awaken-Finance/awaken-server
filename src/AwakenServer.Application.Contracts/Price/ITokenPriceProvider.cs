using System.Threading.Tasks;

namespace AwakenServer.Price;

public interface ITokenPriceProvider
{
    Task<decimal> GetPriceAsync(string pair);
    Task<decimal> GetHistoryPriceAsync(string pair, string dateTime);
}