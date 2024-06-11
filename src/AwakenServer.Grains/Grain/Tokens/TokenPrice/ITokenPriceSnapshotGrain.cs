using System.Threading.Tasks;
using Orleans;

namespace AwakenServer.Grains.Grain.Tokens.TokenPrice;

[Obsolete("This class is deprecated and will be removed in a future version. Use IPriceAppService instead.")]
public interface ITokenPriceSnapshotGrain : IGrainWithStringKey
{
    Task<GrainResultDto<TokenPriceGrainDto>> GetHistoryPriceAsync(string symbol, string dateTime);

}