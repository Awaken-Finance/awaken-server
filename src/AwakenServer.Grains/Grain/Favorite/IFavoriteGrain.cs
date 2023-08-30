using Orleans;

namespace AwakenServer.Grains.Grain.Favorite;

public interface IFavoriteGrain : IGrainWithStringKey
{
    Task<GrainResultDto<FavoriteGrainDto>> CreateAsync(FavoriteGrainDto favoriteDto);
    
    Task<GrainResultDto<FavoriteGrainDto>> DeleteAsync(string id);

    Task<GrainResultDto<List<FavoriteGrainDto>>> GetListAsync();
}