using AwakenServer.ContractEventHandler.Providers;
using AwakenServer.Debits.Entities.Ef;
using AwakenServer.ETOs.Debits;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.ContractEventHandler.Debit.Mappers
{
    public class CTokenRecordMapper : IObjectMapper<CTokenRecord, CTokenRecordChangedEto>, ITransientDependency
    {
        private readonly ICachedDataProvider<CToken> _cTokenInfoProvider;
        private readonly ITokenProvider _tokenProvider;
        private readonly ICachedDataProvider<CompController> _compControllerProvider;
        private readonly IAutoObjectMappingProvider _mapperProvider;

        public CTokenRecordMapper(ICachedDataProvider<CToken> cTokenInfoProvider,
            ICachedDataProvider<CompController> compControllerProvider,
            IAutoObjectMappingProvider mapperProvider, ITokenProvider tokenProvider)
        {
            _cTokenInfoProvider = cTokenInfoProvider;
            _compControllerProvider = compControllerProvider;
            _mapperProvider = mapperProvider;
            _tokenProvider = tokenProvider;
        }

        public CTokenRecordChangedEto Map(CTokenRecord source)
        {
            var recordEto = _mapperProvider.Map<CTokenRecord, CTokenRecordChangedEto>(source);
            var cToken = _cTokenInfoProvider.GetCachedDataById(source.CTokenId);
            recordEto.CToken = cToken;
            recordEto.CompControllerInfo = _compControllerProvider.GetCachedDataById(cToken.CompControllerId);
            recordEto.UnderlyingAssetToken = _tokenProvider.GetToken(cToken.UnderlyingTokenId);
            return recordEto;
        }

        public CTokenRecordChangedEto Map(CTokenRecord source, CTokenRecordChangedEto destination)
        {
            throw new System.NotImplementedException();
        }
    }
}