using AwakenServer.ContractEventHandler.Providers;
using AwakenServer.ETOs.Farms;
using AwakenServer.Farms.Entities.Ef;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.ContractEventHandler.Farm.Mappers
{
    public class FarmRecordMapper : IObjectMapper<FarmRecord, FarmRecordChangedEto>, ITransientDependency
    {
        private readonly ICachedDataProvider<Farms.Entities.Ef.Farm> _farmInfoProvider;
        private readonly ICachedDataProvider<FarmPool> _poolInfoProvider;
        private readonly IAutoObjectMappingProvider _mapperProvider;
        private readonly ITokenProvider _tokenProvider;

        public FarmRecordMapper(ICachedDataProvider<Farms.Entities.Ef.Farm> farmInfoProvider,
            ICachedDataProvider<FarmPool> poolInfoProvider, IAutoObjectMappingProvider mapperProvider,
            ITokenProvider tokenProvider)
        {
            _farmInfoProvider = farmInfoProvider;
            _poolInfoProvider = poolInfoProvider;
            _mapperProvider = mapperProvider;
            _tokenProvider = tokenProvider;
        }

        public FarmRecordChangedEto Map(FarmRecord source)
        {
            var recordEto = _mapperProvider.Map<FarmRecord, FarmRecordChangedEto>(source);
            var pool = _poolInfoProvider.GetCachedDataById(source.PoolId);
            recordEto.FarmInfo = _farmInfoProvider.GetCachedDataById(pool.FarmId);
            recordEto.TokenInfo = _tokenProvider.GetToken(pool.SwapTokenId);
            recordEto.TokenInfo.ChainId = recordEto.FarmInfo.ChainId;
            recordEto.PoolInfo = pool;
            return recordEto;
        }

        public FarmRecordChangedEto Map(FarmRecord source, FarmRecordChangedEto destination)
        {
            throw new System.NotImplementedException();
        }
    }
}