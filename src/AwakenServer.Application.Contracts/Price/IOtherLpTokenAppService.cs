using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AwakenServer.Price.Dtos;
using Volo.Abp.Application.Services;

namespace AwakenServer.Price
{
    public interface IOtherLpTokenAppService : IApplicationService
    {
        Task<OtherLpTokenDto> GetByAddressAsync(string chainId, string address);
        Task<List<OtherLpTokenIndexDto>> GetOtherLpTokenIndexListAsync(string chainId, IEnumerable<string> addresses);
        Task CreateAsync(OtherLpTokenCreateDto input);
        Task UpdateAsync(OtherLpTokenDto input);
    }
}