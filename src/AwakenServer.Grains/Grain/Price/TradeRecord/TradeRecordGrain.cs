using System;
using System.Reflection;
using System.Threading.Tasks;
using AwakenServer.Grains.Grain.Price.TradeRecord;
using AwakenServer.Grains.State.Price;
using AwakenServer.Grains.State.Trade;
using Orleans;
using Orleans.Core;
using Serilog;
using Volo.Abp.EventBus.Local;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.Grains.Grain.Price.TradeRecord;

public class TradeRecordGrain : Grain<TradeRecordState>, ITradeRecordGrain
{
    private readonly IObjectMapper _objectMapper;

    public TradeRecordGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
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

    public async Task<bool> Exist()
    {
        return State.Id != Guid.Empty;
    }

    public async Task<GrainResultDto<TradeRecordGrainDto>> InsertAsync(TradeRecordGrainDto dto)
    {
        //todo remove
        var type = typeof(Grain<TradeRecordState>);
        var fieldInfo1 = type.GetField("_storage", BindingFlags.NonPublic | BindingFlags.Instance);
        var storage = (IStorage<TradeRecordState>)fieldInfo1.GetValue(this);
        //todo remove
        
        Log.Information($"TradeRecordGrain, InsertAsync, etag: {storage.Etag}, recordExist:{storage.RecordExists}, State.Id: {storage.State.Id}, txn: {dto.TransactionHash}, grain id: {this.GetGrainId()}, PrimaryKeyString: {this.GetPrimaryKeyString()}");

        State = _objectMapper.Map<TradeRecordGrainDto, TradeRecordState>(dto);
        
        await WriteStateAsync();

        return new GrainResultDto<TradeRecordGrainDto>()
        {
            Success = true,
            Data = dto
        };
    }
    
    public async Task<GrainResultDto<TradeRecordGrainDto>> GetAsync()
    {
        var dto = _objectMapper.Map<TradeRecordState, TradeRecordGrainDto>(State);
        return new GrainResultDto<TradeRecordGrainDto>()
        {
            Success = true,
            Data = dto
        };
    }
}