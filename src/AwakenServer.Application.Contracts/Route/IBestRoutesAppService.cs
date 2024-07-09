using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AwakenServer.Route.Dtos;
namespace AwakenServer.Route;

public interface IBestRoutesAppService
{
    Task<BestRoutesDto> GetBestRoutesAsync(GetBestRoutesInput input);
    Task<List<long>> GetAmountsInAsync(List<string> tokens, List<Guid> tradePairs, long amountOut);
    Task<List<long>> GetAmountsOutAsync(List<string> tokens, List<Guid> tradePairs, long amountIn);
}