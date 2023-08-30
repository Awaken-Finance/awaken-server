using System;
using AwakenServer.ContractEventHandler.Dividend.AElf.Services;
using AwakenServer.Dividend.Entities.Ef;
using AwakenServer.Dividend.ETOs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.ContractEventHandler.Dividend.Mappers
{
    public class DividendUserRecordMapper : IObjectMapper<DividendUserRecord, DividendUserRecordEto>,
        ITransientDependency
    {
        private readonly IAutoObjectMappingProvider _mapperProvider;
        private readonly ITokenProvider _tokenProvider;
        private readonly IDividendCacheService _dividendCacheService;

        public DividendUserRecordMapper(IAutoObjectMappingProvider mapperProvider,
            IDividendCacheService dividendCacheService, ITokenProvider tokenProvider)
        {
            _mapperProvider = mapperProvider;
            _dividendCacheService = dividendCacheService;
            _tokenProvider = tokenProvider;
        }

        public DividendUserRecordEto Map(DividendUserRecord source)
        {
            var dividendUserRecordEto = _mapperProvider.Map<DividendUserRecord, DividendUserRecordEto>(source);
            dividendUserRecordEto.PoolBaseInfo = _dividendCacheService.GetDividendPoolBaseInfo(source.PoolId);
            if (source.DividendTokenId == Guid.Empty)
            {
                return dividendUserRecordEto;
            }

            dividendUserRecordEto.DividendToken = _tokenProvider.GetToken(source.DividendTokenId);
            dividendUserRecordEto.DividendToken.ChainId = source.ChainId;

            return dividendUserRecordEto;
        }

        public DividendUserRecordEto Map(DividendUserRecord source, DividendUserRecordEto destination)
        {
            throw new System.NotImplementedException();
        }
    }
}