using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AwakenServer.Grains;
using AwakenServer.Grains.Grain.Favorite;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace AwakenServer.Favorite
{
    [RemoteService(IsEnabled = false)]
    public class FavoriteAppService : ApplicationService, IFavoriteAppService
    {
        private readonly IClusterClient _clusterClient;

        public FavoriteAppService(IClusterClient clusterClient)
        {
            _clusterClient = clusterClient;
        }

        public async Task<FavoriteDto> CreateAsync(FavoriteCreateDto input)
        {
            input.Timestamp = DateTimeHelper.ToUnixTimeSeconds(DateTime.UtcNow);
            var favoriteGrain = _clusterClient.GetGrain<IFavoriteGrain>(input.Address);
            var result = await favoriteGrain.CreateAsync(
                ObjectMapper.Map<FavoriteCreateDto, FavoriteGrainDto>(input));
            
            if (!result.Success)
            {
                throw new UserFriendlyException(result.Message);
            }
            
            return ObjectMapper.Map<FavoriteGrainDto, FavoriteDto>(result.Data);
        }
        
        public async Task DeleteAsync(string id)
        {
            var split = GrainIdHelper.SplitByLastSeparator(id);
            if (split == null || string.IsNullOrWhiteSpace(split[1]))
            {
                throw new UserFriendlyException("Invalid id.");
            }

            var favoriteGrain = _clusterClient.GetGrain<IFavoriteGrain>(split[1]);
            var result = await favoriteGrain.DeleteAsync(id);
            
            if (!result.Success)
            {
                throw new UserFriendlyException(result.Message);
            }
        }

        public async Task<List<FavoriteDto>> GetListAsync(string address)
        {
            var favoriteGrain = _clusterClient.GetGrain<IFavoriteGrain>(address);
            var result = await favoriteGrain.GetListAsync();
            return ObjectMapper.Map<List<FavoriteGrainDto>, List<FavoriteDto>>(result.Data);
        }
    }
}