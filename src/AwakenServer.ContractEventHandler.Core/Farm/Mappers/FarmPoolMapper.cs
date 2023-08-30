using System;
using AwakenServer.ContractEventHandler.Providers;
using AwakenServer.ETOs.Farms;
using AwakenServer.Farms.Entities.Ef;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.ContractEventHandler.Farm.Mappers
{
    public class FarmPoolMapper : IObjectMapper<FarmPool, FarmPoolChangedEto>, ITransientDependency
    {
        private readonly ICachedDataProvider<Farms.Entities.Ef.Farm> _farmInfoProvider;
        private readonly IAutoObjectMappingProvider _mapperProvider;
        private readonly ITokenProvider _tokenProvider;

        public FarmPoolMapper(ICachedDataProvider<Farms.Entities.Ef.Farm> farmInfoProvider,
            IAutoObjectMappingProvider mapperProvider,
            ITokenProvider tokenProvider)
        {
            _farmInfoProvider = farmInfoProvider;
            _mapperProvider = mapperProvider;
            _tokenProvider = tokenProvider;
        }

        public FarmPoolChangedEto Map(FarmPool source)
        {
            var swapToken = _tokenProvider.GetToken(source.SwapTokenId);
            var farm = _farmInfoProvider.GetCachedDataById(source.FarmId);
            var targetPool = _mapperProvider.Map<FarmPool, FarmPoolChangedEto>(source);
            targetPool.SwapToken = swapToken;
            if (source.Token1Id != Guid.Empty)
            {
                targetPool.Token1 = _tokenProvider.GetToken(source.Token1Id);
                targetPool.Token1.ChainId = source.ChainId;
            }

            if (source.Token2Id != Guid.Empty)
            {
                targetPool.Token2 = _tokenProvider.GetToken(source.Token2Id);
                targetPool.Token2.ChainId = source.ChainId;
            }

            targetPool.FarmAddress = farm.FarmAddress;
            return targetPool;
        }

        public FarmPoolChangedEto Map(FarmPool source, FarmPoolChangedEto destination)
        {
            throw new System.NotImplementedException();
        }
    }
}