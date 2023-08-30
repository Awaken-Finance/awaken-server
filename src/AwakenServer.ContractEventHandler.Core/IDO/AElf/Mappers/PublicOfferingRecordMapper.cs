using AwakenServer.ContractEventHandler.Providers;
using AwakenServer.IDO.Entities;
using AwakenServer.IDO.Entities.Ef;
using AwakenServer.IDO.ETOs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Threading;

namespace AwakenServer.ContractEventHandler.IDO.AElf.Mappers
{
    public class PublicOfferingRecordMapper : IObjectMapper<PublicOfferingRecord, PublicOfferingRecordEto>,
        ITransientDependency
    {
        private readonly IAutoObjectMappingProvider _mapperProvider;
        private readonly ICachedDataProvider<PublicOffering> _publicOfferingCache;
        private readonly ITokenProvider _tokenProvider;

        public PublicOfferingRecordMapper(IAutoObjectMappingProvider mapperProvider,
            ICachedDataProvider<PublicOffering> publicOfferingCache, ITokenProvider tokenProvider)
        {
            _mapperProvider = mapperProvider;
            _publicOfferingCache = publicOfferingCache;
            _tokenProvider = tokenProvider;
        }

        public PublicOfferingRecordEto Map(PublicOfferingRecord source)
        {
            var dest = _mapperProvider.Map<PublicOfferingRecord, PublicOfferingRecordEto>(source);
            var publicOffering = AsyncHelper.RunSync(async () =>
                await _publicOfferingCache.GetOrSetCachedDataByIdAsync(source.PublicOfferingId));
            dest.PublicOfferingInfo = _mapperProvider.Map<PublicOffering, PublicOfferingWithToken>(publicOffering);
            dest.PublicOfferingInfo.Token = _tokenProvider.GetToken(publicOffering.TokenId);
            dest.PublicOfferingInfo.Token.ChainId = source.ChainId;
            dest.PublicOfferingInfo.RaiseToken = _tokenProvider.GetToken(publicOffering.RaiseTokenId);
            dest.PublicOfferingInfo.RaiseToken.ChainId = source.ChainId;
            return dest;
        }

        public PublicOfferingRecordEto Map(PublicOfferingRecord source, PublicOfferingRecordEto destination)
        {
            throw new System.NotImplementedException();
        }
    }
}