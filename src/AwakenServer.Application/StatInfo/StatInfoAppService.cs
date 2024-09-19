using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.StatInfo.Dtos;
using AwakenServer.StatInfo.Index;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.StatInfo;

public class StatInfoAppService : ApplicationService, IStatInfoAppService
{
    private readonly INESTRepository<StatInfoSnapshotIndex, Guid> _statInfoSnapshotIndexRepository;
    private readonly ILogger<StatInfoAppService> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly StatInfoOptions _statInfoOptions;

    public StatInfoAppService(INESTRepository<StatInfoSnapshotIndex, Guid> statInfoSnapshotIndexRepository,
        IObjectMapper objectMapper,
        ILogger<StatInfoAppService> logger,
        IOptionsSnapshot<StatInfoOptions> statInfoPeriodOptions)
    {
        _statInfoSnapshotIndexRepository = statInfoSnapshotIndexRepository;
        _logger = logger;
        _objectMapper = objectMapper;
        _statInfoOptions = statInfoPeriodOptions.Value;
    }

    public async Task<ListResultDto<StatInfoTvlDto>> GetTvlListAsync(GetStatHistoryInput input)
    {
        return null;
    }

    public async Task<ListResultDto<StatInfoPriceDto>> GetPriceListAsync(GetStatHistoryInput input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<StatInfoSnapshotIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(input.ChainId)));
        if (string.IsNullOrEmpty(input.Symbol) && string.IsNullOrEmpty(input.PairAddress))
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.StatType).Value(0)));
        }
        else if (!string.IsNullOrEmpty(input.Symbol))
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Symbol).Value(input.Symbol)));
            mustQuery.Add(q => q.Term(i => i.Field(f => f.StatType).Value(1)));
        }
        else if (!string.IsNullOrEmpty(input.PairAddress))
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.PairAddress).Value(input.PairAddress)));
            mustQuery.Add(q => q.Term(i => i.Field(f => f.StatType).Value(2)));

        }

        // todo get time range by period type
        
        var periodType = (PeriodType)input.PeriodType;
        var period = _statInfoOptions.TypePeriodMapping[periodType.ToString()];
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Period).Value(period)));
        
        QueryContainer Filter(QueryContainerDescriptor<StatInfoSnapshotIndex> f) => f.Bool(b => b.Must(mustQuery));
        var list = await _statInfoSnapshotIndexRepository.GetListAsync(Filter, sortExp: k => k.Timestamp);
        return new ListResultDto<StatInfoPriceDto>
        {
            Items = _objectMapper.Map<List<StatInfoSnapshotIndex>, List<StatInfoPriceDto>>(list.Item2)
        };
    }

    public async Task<ListResultDto<StatInfoVolumeDto>> GetVolumeListAsync(GetStatHistoryInput input)
    {
        return null;
    }
}