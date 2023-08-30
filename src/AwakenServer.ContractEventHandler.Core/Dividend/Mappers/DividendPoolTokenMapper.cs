using AwakenServer.ContractEventHandler.Dividend.AElf.Services;
using AwakenServer.Dividend.ETOs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.ContractEventHandler.Dividend.Mappers
{
    public class DividendPoolTokenMapper : IObjectMapper<AwakenServer.Dividend.Entities.Ef.DividendPoolToken, DividendPoolTokenEto>, ITransientDependency
    {
        private readonly IAutoObjectMappingProvider _mapperProvider;
        private readonly ITokenProvider _tokenProvider;
        private readonly IDividendCacheService _dividendCacheService;

        public DividendPoolTokenMapper(IAutoObjectMappingProvider mapperProvider,
            IDividendCacheService dividendCacheService, ITokenProvider tokenProvider)
        {
            _mapperProvider = mapperProvider;
            _dividendCacheService = dividendCacheService;
            _tokenProvider = tokenProvider;
        }

        public DividendPoolTokenEto Map(AwakenServer.Dividend.Entities.Ef.DividendPoolToken source)
        {
            var poolTokenEto = _mapperProvider.Map<AwakenServer.Dividend.Entities.Ef.DividendPoolToken, DividendPoolTokenEto>(source);
            poolTokenEto.PoolBaseInfo = _dividendCacheService.GetDividendPoolBaseInfo(source.PoolId);
            poolTokenEto.DividendToken = _tokenProvider.GetToken(source.DividendTokenId);
            poolTokenEto.DividendToken.ChainId = source.ChainId;
            return poolTokenEto;
        }

        public DividendPoolTokenEto Map(AwakenServer.Dividend.Entities.Ef.DividendPoolToken source, DividendPoolTokenEto destination)
        {
            throw new System.NotImplementedException();
        }
    }
}