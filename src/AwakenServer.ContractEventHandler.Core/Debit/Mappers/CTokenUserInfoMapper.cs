using AwakenServer.ContractEventHandler.Providers;
using AwakenServer.Debits.Entities.Ef;
using AwakenServer.ETOs.Debits;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.ContractEventHandler.Debit.Mappers
{
    public class CTokenUserInfoMapper : IObjectMapper<CTokenUserInfo, CTokenUserInfoChangedEto>, ITransientDependency
    {
        private readonly ICachedDataProvider<CToken> _cTokenInfoProvider;
        private readonly ITokenProvider _tokenProvider;
        private readonly ICachedDataProvider<CompController> _compControllerProvider;
        private readonly IAutoObjectMappingProvider _mapperProvider;

        public CTokenUserInfoMapper(IAutoObjectMappingProvider mapperProvider,
            ICachedDataProvider<CToken> cTokenInfoProvider, ICachedDataProvider<CompController> compControllerProvider,
            ITokenProvider tokenProvider)
        {
            _mapperProvider = mapperProvider;
            _cTokenInfoProvider = cTokenInfoProvider;
            _compControllerProvider = compControllerProvider;
            _tokenProvider = tokenProvider;
        }

        public CTokenUserInfoChangedEto Map(CTokenUserInfo source)
        {
            var userInfo = _mapperProvider.Map<CTokenUserInfo, CTokenUserInfoChangedEto>(source);
            var cToken = _cTokenInfoProvider.GetCachedDataById(source.CTokenId);
            userInfo.CTokenInfo = cToken;
            userInfo.CompInfo = _compControllerProvider.GetCachedDataById(cToken.CompControllerId);
            userInfo.UnderlyingToken = _tokenProvider.GetToken(cToken.UnderlyingTokenId);
            return userInfo;
        }

        public CTokenUserInfoChangedEto Map(CTokenUserInfo source, CTokenUserInfoChangedEto destination)
        {
            return Map(source);
        }
    }
}