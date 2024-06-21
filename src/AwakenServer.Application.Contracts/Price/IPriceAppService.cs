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
        public Task<string> GetTokenPriceAsync(GetTokenPriceInput input);
        Task<ListResultDto<TokenPriceDataDto>> GetTokenPriceListAsync(List<string> symbols);
        Task<ListResultDto<TokenPriceDataDto>> GetTokenHistoryPriceDataAsync(List<GetTokenHistoryPriceInput> inputs);
        
    }
    
}