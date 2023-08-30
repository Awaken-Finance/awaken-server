using AwakenServer.ContractEventHandler.Dividend.AElf.Services;
using AwakenServer.Dividend.Entities.Ef;
using AwakenServer.Dividend.ETOs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.ContractEventHandler.Dividend.Mappers
{
    public class DividendTokenMapper : IObjectMapper<DividendToken, DividendTokenEto>, ITransientDependency
    {
        private readonly IAutoObjectMappingProvider _mapperProvider;
        private readonly ITokenProvider _tokenProvider;
        private readonly IDividendCacheService _dividendCacheService;

        public DividendTokenMapper(IAutoObjectMappingProvider mapperProvider,
            IDividendCacheService dividendCacheService, ITokenProvider tokenProvider)
        {
            _mapperProvider = mapperProvider;
            _dividendCacheService = dividendCacheService;
            _tokenProvider = tokenProvider;
        }

        public DividendTokenEto Map(DividendToken source)
        {
            var dividendTokenEto = _mapperProvider.Map<DividendToken, DividendTokenEto>(source);
            dividendTokenEto.Dividend = _dividendCacheService.GetDividendBaseInfo(source.DividendId);
            dividendTokenEto.Token = _tokenProvider.GetToken(source.TokenId);
            dividendTokenEto.Token.ChainId = source.ChainId;
            return dividendTokenEto;
        }

        public DividendTokenEto Map(DividendToken source, DividendTokenEto destination)
        {
            throw new System.NotImplementedException();
        }
    }
}