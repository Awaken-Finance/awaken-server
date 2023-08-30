using AwakenServer.IDO.Entities.Ef;
using AwakenServer.IDO.ETOs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.ContractEventHandler.IDO.AElf.Mappers
{
    public class PublicOfferingMapper : IObjectMapper<PublicOffering, PublicOfferingEto>, ITransientDependency
    {
        private readonly IAutoObjectMappingProvider _mapperProvider;
        private readonly ITokenProvider _tokenProvider;

        public PublicOfferingMapper(IAutoObjectMappingProvider mapperProvider, ITokenProvider tokenProvider)
        {
            _mapperProvider = mapperProvider;
            _tokenProvider = tokenProvider;
        }

        public PublicOfferingEto Map(PublicOffering source)
        {
            var dest = _mapperProvider.Map<PublicOffering, PublicOfferingEto>(source);
            dest.Token = _tokenProvider.GetToken(source.TokenId);
            dest.Token.ChainId = source.ChainId;
            dest.RaiseToken = _tokenProvider.GetToken(source.RaiseTokenId);
            dest.RaiseToken.ChainId = source.ChainId;
            return dest;
        }

        public PublicOfferingEto Map(PublicOffering source, PublicOfferingEto destination)
        {
            throw new System.NotImplementedException();
        }
    }
}