using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AwakenServer.Price.Dtos;

namespace AwakenServer.Farms.Services
{
    public interface IFarmAppPriceService
    {
        public Task<List<FarmPriceDto>> GetSwapTokenPricesAsync(GetSwapTokenPricesInput input);
        public Task<string> GetTokenPriceAsync(GetTokenPriceInput input);
    }

    public class GetSwapTokenPricesInput
    {
        public string ChainId { get; set; }
        public string[] TokenAddresses { get; set; }
        public string[] TokenSymbol { get; set; }
    }
}