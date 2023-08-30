// using System;
// using System.Threading.Tasks;
// using AwakenServer.GameOfTrust;
// using AwakenServer.GameOfTrust.DTos;
// using AwakenServer.GameOfTrust.DTos.Dto;
// using AwakenServer.GameOfTrust.DTos.Input;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.Extensions.Logging;
// using Volo.Abp;
// using Volo.Abp.Application.Dtos;
// using Volo.Abp.AspNetCore.Mvc;
//
// namespace AwakenServer.Controllers.GameOfTrust
// {   
//     [RemoteService]
//     [Area("app")]
//     [ControllerName("GameOfTrust")]
//     [Route("api/app/game-of-trusts")]
//     public class GameOfTrustController: AbpController
//     {
//         private readonly IGameOfTrustService _gameService;
//         private readonly ILogger<GameOfTrustController> _logger;
//
//         public GameOfTrustController(IGameOfTrustService gameService, ILogger<GameOfTrustController> logger)
//         {
//             _gameService = gameService;
//             _logger = logger;
//         }
//         
//         
//         
//         [HttpGet]
//         public virtual Task<PagedResultDto<GameOfTrustDto>> GetGameOfTrustListAsync(GetGameListInput input)
//         {
//             return _gameService.GetGameOfTrustsAsync(input);
//         }
//
//         
//
//         [HttpGet]
//         [Route("{id}")]
//         public virtual Task<GameOfTrustDto> GetAsync(Guid id)
//         {
//             return _gameService.GetAsync(id);
//         }
//
//         
//         [HttpGet]
//         [Route("market-data")]
//         public virtual Task<PagedResultDto<MarketDataDto>> GetMarketDatasAsync(GetMarketDataInput input)
//         {
//             return _gameService.GetMarketDatasAsync(input);
//         }
//         
//
//         [HttpGet]
//         [Route("market-caps")]
//         public virtual Task<ListResultDto<MarketCapsDto>> GetMarketCapsAsync(GetMarketCapsInput input)
//         {
//             return _gameService.GetMarketCapsAsync(input);
//         }
//      
//     }
// }