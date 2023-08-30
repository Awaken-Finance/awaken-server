using AwakenServer.ContractEventHandler.Dividend.AElf.Services;
using AwakenServer.Dividend.Entities.Ef;
using AwakenServer.Dividend.ETOs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.ContractEventHandler.Dividend.Mappers
{
    public class DividendPoolMapper : IObjectMapper<DividendPool, DividendPoolEto>, ITransientDependency
    {
        private readonly IAutoObjectMappingProvider _mapperProvider;
        private readonly IDividendCacheService _dividendCacheService;
        private readonly ITokenProvider _tokenProvider;

        public DividendPoolMapper(IAutoObjectMappingProvider mapperProvider,
            IDividendCacheService dividendCacheService, ITokenProvider tokenProvider)
        {
            _mapperProvider = mapperProvider;
            _dividendCacheService = dividendCacheService;
            _tokenProvider = tokenProvider;
        }

        public DividendPoolEto Map(DividendPool source)
        {
            var poolEto = _mapperProvider.Map<DividendPool, DividendPoolEto>(source);
            poolEto.PoolToken = _tokenProvider.GetToken(source.PoolTokenId);
            poolEto.PoolToken.ChainId = source.ChainId;
            poolEto.Dividend = _dividendCacheService.GetDividendBaseInfo(source.DividendId);
            return poolEto;
        }

        public DividendPoolEto Map(DividendPool source, DividendPoolEto destination)
        {
            throw new System.NotImplementedException();
        }
    }
}