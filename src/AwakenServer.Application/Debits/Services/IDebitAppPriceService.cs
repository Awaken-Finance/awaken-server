using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AwakenServer.Price.Dtos;

namespace AwakenServer.Debits.Services
{
    public interface IDebitAppPriceService
    {
        public Task<List<UnderlyingTokenPriceDto>> GetCTokenPricesAsync(GetTokenPricesInput input);
        public Task<List<UnderlyingTokenPriceDto>> GetUnderlyingTokenPricesAsync(GetTokenPricesInput input);
        Task<string> GetTokenPricesAsync(GetTokenPriceInput input);
    }
    
    public class UnderlyingTokenPriceDto
    {
        public string ChainId { get; set; }
        public string TokenSymbol { get; set; }
        public string TokenAddress { get; set; }
        public string Price { get; set; }
    }

    public class GetTokenPricesInput
    {
        public string ChainId { get; set; }
        public string[] TokenAddresses { get; set; }
        public string[] TokenSymbol { get; set; }
    }
}