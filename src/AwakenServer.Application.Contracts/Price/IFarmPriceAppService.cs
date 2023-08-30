using System.Collections.Generic;
using System.Threading.Tasks;
using AwakenServer.Price.Dtos;
using Volo.Abp.Application.Services;

namespace AwakenServer.Price
{
    public interface IFarmPriceAppService : IApplicationService
    {
        public Task<List<FarmPriceDto>> GetPricesAsync(GetFarmTokenPriceInput input);
    }
}