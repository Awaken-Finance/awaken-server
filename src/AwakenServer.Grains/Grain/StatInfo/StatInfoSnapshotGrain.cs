using Orleans;
using Volo.Abp.ObjectMapping;
using AwakenServer.Grains.State.StatInfo;

namespace AwakenServer.Grains.Grain.StatInfo;

public class StatInfoSnapshotGrain : Grain<StatInfoSnapshotState>, IStatInfoSnapshotGrain
{
    private readonly IObjectMapper _objectMapper;

    public StatInfoSnapshotGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public async Task<GrainResultDto<StatInfoSnapshotGrainDto>> AddOrUpdateAsync(StatInfoSnapshotGrainDto dto)
    {
        if (State.Timestamp == dto.Timestamp)
        {
            if (dto.PriceInUsd > 0)
            {
                State.PriceInUsd = dto.PriceInUsd;
            }
            
            if (dto.Price > 0)
            {
                State.Price = dto.Price;
            }

            if (dto.Tvl > 0)
            {
                State.Tvl = dto.Tvl;
            }

            if (dto.VolumeInUsd > 0)
            {
                State.VolumeInUsd += dto.VolumeInUsd;
            }

            if (dto.LpFeeInUsd > 0)
            {
                State.LpFeeInUsd += dto.LpFeeInUsd;
            }
        }
        else
        {
            State = _objectMapper.Map<StatInfoSnapshotGrainDto, StatInfoSnapshotState>(dto);
            State.Id = Guid.NewGuid();
        }
        
        await WriteStateAsync();
        return new GrainResultDto<StatInfoSnapshotGrainDto>()
        {
            Success = true,
            Data = _objectMapper.Map<StatInfoSnapshotState, StatInfoSnapshotGrainDto>(State)
        };
    }
}