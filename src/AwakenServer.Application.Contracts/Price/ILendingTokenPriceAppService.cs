using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AwakenServer.Price.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace AwakenServer.Price
{
    public interface ILendingTokenPriceAppService : IApplicationService
    {
        Task CreateOrUpdateAsync(LendingTokenPriceCreateOrUpdateDto input);

        Task<LendingTokenPriceDto> GetByTokenIdAsync(Guid tokenId);

        Task<List<LendingTokenPriceIndexDto>> GetPricesAsync(GetPricesInput input);
        
        Task<List<LendingTokenPriceIndexDto>> GetPricesAsync(string chainId, string[] tokenAddresses);

        Task<PagedResultDto<LendingTokenPriceHistoryIndexDto>> GetPriceHistoryAsync(GetPriceHistoryInput input);
    }
}