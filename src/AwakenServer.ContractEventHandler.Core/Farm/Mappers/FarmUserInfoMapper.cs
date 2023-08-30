using System;
using AwakenServer.ContractEventHandler.Providers;
using AwakenServer.ETOs.Farms;
using AwakenServer.Farms.Entities.Ef;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.ContractEventHandler.Farm.Mappers
{
    public class FarmUserInfoMapper : IObjectMapper<FarmUserInfo, FarmUserInfoChangedEto>, ITransientDependency
    {
        private readonly ICachedDataProvider<Farms.Entities.Ef.Farm> _farmInfoProvider;
        private readonly ICachedDataProvider<FarmPool> _poolInfoProvider;
        private readonly IAutoObjectMappingProvider _mapperProvider;
        private readonly ITokenProvider _tokenProvider;

        public FarmUserInfoMapper(
            ICachedDataProvider<Farms.Entities.Ef.Farm> farmInfoProvider,
            ICachedDataProvider<FarmPool> poolInfoProvider,
            IAutoObjectMappingProvider mapperProvider, ITokenProvider tokenProvider)
        {
            _farmInfoProvider = farmInfoProvider;
            _poolInfoProvider = poolInfoProvider;
            _mapperProvider = mapperProvider;
            _tokenProvider = tokenProvider;
        }

        public FarmUserInfoChangedEto Map(FarmUserInfo source)
        {
            var pool = _poolInfoProvider.GetCachedDataById(source.PoolId);
            var farm = _farmInfoProvider.GetCachedDataById(pool.FarmId);
            var swapToken = _tokenProvider.GetToken(pool.SwapTokenId);
            var targetUser = _mapperProvider.Map<FarmUserInfo, FarmUserInfoChangedEto>(source);
            targetUser.FarmInfo = farm;
            targetUser.SwapToken = swapToken;
            if (pool.Token1Id != Guid.Empty)
            {
                targetUser.Token1 = _tokenProvider.GetToken(pool.Token1Id);
                targetUser.Token1.ChainId = source.ChainId;
            }

            if (pool.Token2Id != Guid.Empty)
            {
                targetUser.Token2 = _tokenProvider.GetToken(pool.Token2Id);
                targetUser.Token2.ChainId = source.ChainId;
            }

            targetUser.PoolInfo = pool;
            return targetUser;
        }

        public FarmUserInfoChangedEto Map(FarmUserInfo source, FarmUserInfoChangedEto destination)
        {
            throw new System.NotImplementedException();
        }
    }
}