using System;
using AwakenServer.ContractEventHandler.Dividend.AElf.Services;
using AwakenServer.Dividend.Entities.Ef;
using AwakenServer.Dividend.ETOs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.ContractEventHandler.Dividend.Mappers
{
    public class UserDividendInfoMapper : IObjectMapper<DividendUserPool, DividendUserPoolEto>, ITransientDependency
    {
        private readonly IAutoObjectMappingProvider _mapperProvider;
        private readonly IDividendCacheService _dividendCacheService;

        public UserDividendInfoMapper(IAutoObjectMappingProvider mapperProvider,
            IDividendCacheService dividendCacheService)
        {
            _mapperProvider = mapperProvider;
            _dividendCacheService = dividendCacheService;
        }

        public DividendUserPoolEto Map(DividendUserPool source)
        {
            var userDividendInfoEto = _mapperProvider.Map<DividendUserPool, DividendUserPoolEto>(source);
            userDividendInfoEto.PoolBaseInfo = _dividendCacheService.GetDividendPoolBaseInfo(source.PoolId);
            return userDividendInfoEto;
        }

        public DividendUserPoolEto Map(DividendUserPool source, DividendUserPoolEto destination)
        {
            throw new NotImplementedException();
        }
    }
}