using AwakenServer.Grains.State.Favorite;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.Grains.Grain.Favorite;

public class FavoriteGrain : Grain<FavoriteState>, IFavoriteGrain
{
    private readonly IObjectMapper _objectMapper;

    public FavoriteGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
        await base.OnActivateAsync();
    }

    public override async Task OnDeactivateAsync()
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync();
    }

    public async Task<GrainResultDto<FavoriteGrainDto>> CreateAsync(FavoriteGrainDto favoriteDto)
    {
        var result = new GrainResultDto<FavoriteGrainDto>();
        
        favoriteDto.Id = GrainIdHelper.GenerateGrainId(favoriteDto.TradePairId, favoriteDto.Address);
        if (State.FavoriteInfos.Exists(info => info.Id == favoriteDto.Id))
        {
            result.Message = FavoriteMessage.ExistedMessage;
            return result;
        }
        
        if(State.FavoriteInfos.Count >= FavoriteMessage.MaxLimit)
        {
            result.Message = FavoriteMessage.ExceededMessage;
            return result;
        }
        State.Id = this.GetPrimaryKeyString();
        State.FavoriteInfos.Add(_objectMapper.Map<FavoriteGrainDto, FavoriteInfo>(favoriteDto));
        
        await WriteStateAsync();

        result.Success = true;
        result.Data = favoriteDto;
        return result;
    }

    public async Task<GrainResultDto<FavoriteGrainDto>> DeleteAsync(string id)
    {
        var result = new GrainResultDto<FavoriteGrainDto>();
        
        if (!State.FavoriteInfos.Exists(info => info?.Id == id))
        {
            result.Message = FavoriteMessage.NotExistMessage;
            return result;
        }
        
        var info = State.FavoriteInfos.FirstOrDefault(info => info.Id == id);
        if (info != null)
        {
            State.FavoriteInfos.Remove(info);
            await WriteStateAsync();
        }
        
        result.Success = true;
        result.Data = null;
        return result;
    }
    
    public async Task<GrainResultDto<List<FavoriteGrainDto>>> GetListAsync()
    {
        return new GrainResultDto<List<FavoriteGrainDto>>
        {
            Success = true,
            Data = _objectMapper.Map<List<FavoriteInfo>, List<FavoriteGrainDto>>(State.FavoriteInfos)
        };
    }
}