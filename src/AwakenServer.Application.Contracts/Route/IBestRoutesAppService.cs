using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AwakenServer.Route.Dtos;
namespace AwakenServer.Route;

public interface IBestRoutesAppService
{
    Task<BestRoutesDto> GetBestRoutesAsync(GetBestRoutesInput input);
    Task ClearRoutesCacheAsync(string chainId);
}