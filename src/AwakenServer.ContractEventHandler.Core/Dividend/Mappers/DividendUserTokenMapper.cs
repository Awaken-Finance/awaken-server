using AwakenServer.ContractEventHandler.Dividend.AElf.Services;
using AwakenServer.Dividend.Entities.Ef;
using AwakenServer.Dividend.ETOs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.ContractEventHandler.Dividend.Mappers
{
    public class DividendUserTokenMapper : IObjectMapper<DividendUserToken, DividendUserTokenEto>, ITransientDependency
    {
        private readonly IAutoObjectMappingProvider _mapperProvider;
        private readonly ITokenProvider _tokenProvider;
        private readonly IDividendCacheService _dividendCacheService;

        public DividendUserTokenMapper(IAutoObjectMappingProvider mapperProvider,
            IDividendCacheService dividendCacheService, ITokenProvider tokenProvider)
        {
            _mapperProvider = mapperProvider;
            _dividendCacheService = dividendCacheService;
            _tokenProvider = tokenProvider;
        }

        public DividendUserTokenEto Map(DividendUserToken source)
        {
            var dividendUserTokenEto = _mapperProvider.Map<DividendUserToken, DividendUserTokenEto>(source);
            dividendUserTokenEto.PoolBaseInfo = _dividendCacheService.GetDividendPoolBaseInfo(source.PoolId);
            dividendUserTokenEto.DividendToken = _tokenProvider.GetToken(source.DividendTokenId);
            dividendUserTokenEto.DividendToken.ChainId = source.ChainId;
            return dividendUserTokenEto;
        }

        public DividendUserTokenEto Map(DividendUserToken source, DividendUserTokenEto destination)
        {
            throw new System.NotImplementedException();
        }
    }
}