using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AwakenServer.Price.Dtos;
using AwakenServer.Tokens.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;


namespace AwakenServer.Price
{
    public interface IPriceAppService : IApplicationService
    {
        Task<Tuple<TokenPriceDataDto, TokenPriceDataDto>> GetPairTokenPriceAsync(string chainId, Guid tradePairId,
            string symbol0,
            string symbol1);
        Task<string> GetTokenPriceAsync(GetTokenPriceInput input);
        Task<ListResultDto<TokenPriceDataDto>> GetTokenPriceListAsync(List<string> symbols);
        Task<ListResultDto<TokenPriceDataDto>> GetTokenHistoryPriceDataAsync(List<GetTokenHistoryPriceInput> inputs);
        Task RebuildPricingMapAsync(string chainId);
        Task UpdatePricingMapAsync(string chainId, Guid tradePairId, string token0Amount, string token1Amount);
    }
}