using System;
using System.Threading.Tasks;
using AwakenServer.GameOfTrust.DTos;
using AwakenServer.GameOfTrust.DTos.Dto;
using AwakenServer.GameOfTrust.DTos.Input;
using AwakenServer.Trade.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace AwakenServer.GameOfTrust
{
    public interface IGameOfTrustService : IApplicationService 
    {
        Task<PagedResultDto<GameOfTrustDto>> GetGameOfTrustsAsync(GetGameListInput input);
       
        Task<GameOfTrustDto> GetAsync(Guid id);
        
        Task<PagedResultDto<MarketDataDto>> GetMarketDatasAsync(GetMarketDataInput input);
        
        Task<PagedResultDto<UserGameofTrustDto>> GetUserGameOfTrustsAsync(GetUserGameOfTrustsInput input);
       
        Task<UserAssetDto> GetUserAssertAsync(GetUserAssertInput input);

        Task<PagedResultDto<GetUserGameOfTrustRecordDto>> GetUserGameOfTrustRecord(GetUserGameOfTrustRecordInput input);
        
        Task<ListResultDto<MarketCapsDto>> GetMarketCapsAsync(GetMarketCapsInput input);
    }
}