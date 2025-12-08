using System.Collections.Generic;
using System.Threading.Tasks;
using AwakenServer.Common;
using AwakenServer.Grains.State.Price;
using Microsoft.Extensions.Logging;
using Volo.Abp.Caching;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.Grains.Grain.Price.TradeRecord;

public class UnconfirmedTransactionsGrain : Grain<UnconfirmedTransactionsState>, IUnconfirmedTransactionsGrain
{

    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<UnconfirmedTransactionsGrain> _logger;
    
    public UnconfirmedTransactionsGrain(IObjectMapper objectMapper,
        ILogger<UnconfirmedTransactionsGrain> logger)
    {
        _objectMapper = objectMapper;
        _logger = logger;
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
    
    public async Task<GrainResultDto<UnconfirmedTransactionsGrainDto>> AddAsync(UnconfirmedTransactionsGrainDto dto)
    {
        State.MinUnconfirmedBlockHeight = State.MinUnconfirmedBlockHeight == 0 ? dto.BlockHeight : Math.Min(State.MinUnconfirmedBlockHeight, dto.BlockHeight);
        if (!State.UnconfirmedTransactions.ContainsKey(dto.BlockHeight))
        {
            State.UnconfirmedTransactions.Add(dto.BlockHeight, new List<ToBeConfirmRecord>());
        }
        State.UnconfirmedTransactions[dto.BlockHeight].Add(_objectMapper.Map<UnconfirmedTransactionsGrainDto, ToBeConfirmRecord>(dto));
        await WriteStateAsync();
        return new GrainResultDto<UnconfirmedTransactionsGrainDto>
        {
            Success = true,
            Data = dto
        };
    }
    
    public async Task<GrainResultDto<List<UnconfirmedTransactionsGrainDto>>> GetAsync(EventType type, long startBlock, long endBlock)
    {
        var list = new List<UnconfirmedTransactionsGrainDto>();
        
        foreach (var blockUnconfiemd in State.UnconfirmedTransactions)
        {
            if (blockUnconfiemd.Key < startBlock || blockUnconfiemd.Key > endBlock)
            {
                continue;
            }

            foreach (var txn in blockUnconfiemd.Value)
            {
                list.Add(new UnconfirmedTransactionsGrainDto
                {
                    BlockHeight = blockUnconfiemd.Key,
                    TransactionHash = txn.TransactionHash
                });
            }
        }

        if (!State.UnconfirmedTransactions.IsNullOrEmpty())
        {
            for (var i = startBlock; i <= endBlock; i++)
            {
                State.UnconfirmedTransactions.Remove(i);
            }
        }
        
        State.MinUnconfirmedBlockHeight = endBlock;
        
        await WriteStateAsync();
        
        return new GrainResultDto<List<UnconfirmedTransactionsGrainDto>>
        {
            Success = true,
            Data = list
        };
    }
    
    
    public async Task<long> GetMinUnconfirmedHeightAsync()
    {
        return State.MinUnconfirmedBlockHeight;
    }

}