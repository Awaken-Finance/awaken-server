// using System.Threading.Tasks;
// using AwakenServer.GameOfTrust;
// using AwakenServer.GameOfTrust.DTos;
// using AwakenServer.GameOfTrust.DTos.Dto;
// using AwakenServer.GameOfTrust.DTos.Input;
// using AwakenServer.Trade.Dtos;
// using Microsoft.AspNetCore.Mvc;
// using Volo.Abp;
// using Volo.Abp.Application.Dtos;
// using Volo.Abp.AspNetCore.Mvc;
//
// namespace AwakenServer.Controllers.GameOfTrust
// {   
//     [RemoteService]
//     [Area("app")]
//     [ControllerName("GameOfTrustUser")]
//     [Microsoft.AspNetCore.Components.Route("api/app/user-game-of-trusts")]
//     public class GameOfTrustUserController: AbpController
//     {
//         private readonly IGameOfTrustService _gameOfTrustService;
//
//         public GameOfTrustUserController(IGameOfTrustService gameOfTrustService)
//         {
//             _gameOfTrustService = gameOfTrustService;
//         }
//         
//       
//         [HttpGet]
//         public virtual Task<PagedResultDto<UserGameofTrustDto>> GetUserGameOfTrustsAsync(GetUserGameOfTrustsInput input)
//         {
//             return _gameOfTrustService.GetUserGameOfTrustsAsync(input);
//         }
//         
//         [HttpGet]
//         public virtual Task<UserAssetDto> GetUserAssetAsync(GetUserAssertInput input)
//         {
//             return _gameOfTrustService.GetUserAssertAsync(input);
//         }
//
//
//         public virtual Task<PagedResultDto<GetUserGameOfTrustRecordDto>> GetUserGameOfTrustRecord(GetUserGameOfTrustRecordInput input)
//         {
//             return _gameOfTrustService.GetUserGameOfTrustRecord(input);
//         }
//         
//     }
// }