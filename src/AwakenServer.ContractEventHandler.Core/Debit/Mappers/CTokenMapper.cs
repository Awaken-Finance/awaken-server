using AwakenServer.Debits.Entities.Ef;
using AwakenServer.ETOs.Debits;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.ContractEventHandler.Debit.Mappers
{
    public class CTokenMapper : IObjectMapper<CToken, CTokenChangedEto>, ITransientDependency
    {
        private readonly IAutoObjectMappingProvider _mapperProvider;
        private readonly ITokenProvider _tokenProvider;

        public CTokenMapper(IAutoObjectMappingProvider mapperProvider, ITokenProvider tokenProvider)
        {
            _mapperProvider = mapperProvider;
            _tokenProvider = tokenProvider;
        }

        public CTokenChangedEto Map(CToken source)
        {
            var cTokenEto = _mapperProvider.Map<CToken, CTokenChangedEto>(source);
            var underlyingToken = _tokenProvider.GetToken(source.UnderlyingTokenId);
            cTokenEto.UnderlyingToken = underlyingToken;
            return cTokenEto;
        }

        public CTokenChangedEto Map(CToken source, CTokenChangedEto destination)
        {
            return Map(source);
        }
    }
}