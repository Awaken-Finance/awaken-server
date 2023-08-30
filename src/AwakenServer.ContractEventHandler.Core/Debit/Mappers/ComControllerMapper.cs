using AwakenServer.Debits.Entities.Ef;
using AwakenServer.ETOs.Debits;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.ContractEventHandler.Debit.Mappers
{
    public class ComControllerMapper: IObjectMapper<CompController, CompControllerChangedEto>, ITransientDependency
    {
        private readonly IAutoObjectMappingProvider _mapperProvider;
        private readonly ITokenProvider _tokenProvider;

        public ComControllerMapper(IAutoObjectMappingProvider mapperProvider, ITokenProvider tokenProvider)
        {
            _mapperProvider = mapperProvider;
            _tokenProvider = tokenProvider;
        }

        public CompControllerChangedEto Map(CompController source)
        {
            var controller = _mapperProvider.Map<CompController, CompControllerChangedEto>(source);
            controller.DividendToken = _tokenProvider.GetToken(source.DividendTokenId);
            return controller;
        }

        public CompControllerChangedEto Map(CompController source, CompControllerChangedEto destination)
        {
            return Map(source);
        }
    }
}