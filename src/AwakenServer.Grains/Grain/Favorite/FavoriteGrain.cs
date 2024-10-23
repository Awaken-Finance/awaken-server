using AwakenServer.Grains.State.Favorite;
using Newtonsoft.Json;
using Serilog;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.Grains.Grain.Favorite;

public class FavoriteGrain : Grain<FavoriteState>, IFavoriteGrain
{
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger _logger;

    public FavoriteGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
        _logger = Log.ForContext<FavoriteGrain>();
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync(reason, cancellationToken);
    }

    public async Task<GrainResultDto<FavoriteGrainDto>> CreateAsync(FavoriteGrainDto favoriteDto)
    {
        _logger.Information($"FavoriteGrain add, user address {favoriteDto.Address}, trade pair id {favoriteDto.TradePairId}");
        var result = new GrainResultDto<FavoriteGrainDto>();
        
        favoriteDto.Id = GrainIdHelper.GenerateGrainId(favoriteDto.TradePairId, favoriteDto.Address);
        if (State.FavoriteInfos.Exists(info => info.Id == favoriteDto.Id))
        {
            _logger.Information($"exist fav info, {JsonConvert.SerializeObject(State.FavoriteInfos)}");
            result.Message = FavoriteMessage.ExistedMessage;
            return result;
        }
        
        if(State.FavoriteInfos.Count >= FavoriteMessage.MaxLimit)
        {
            _logger.Information($"fav list size out of range, now: {State.FavoriteInfos.Count}, limit: {FavoriteMessage.MaxLimit}");
            result.Message = FavoriteMessage.ExceededMessage;
            return result;
        }
        State.Id = this.GetPrimaryKeyString();
        State.FavoriteInfos.Add(_objectMapper.Map<FavoriteGrainDto, FavoriteInfo>(favoriteDto));
        
        await WriteStateAsync();

        result.Success = true;
        result.Data = favoriteDto;
        
        _logger.Information($"FavoriteGrain add, user address {favoriteDto.Address}, trade pair id {favoriteDto.TradePairId} done");
        
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