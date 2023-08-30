using AwakenServer.ContractEventHandler.Providers;
using AwakenServer.ETOs.GameOfTrust;
using AwakenServer.Tokens;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.ContractEventHandler.GameOfTrust.Mappers
{
    public class GameOfTrustMapper: IObjectMapper<Entities.GameOfTrust.Ef.GameOfTrust,GameChangedEto>,ITransientDependency
    {
        private readonly ITokenProvider _tokenProvider;
        private readonly IAutoObjectMappingProvider _mapperProvider;

        public GameOfTrustMapper(ITokenProvider tokenProvider, IAutoObjectMappingProvider mapperProvider)
        {
            _tokenProvider = tokenProvider;
            _mapperProvider = mapperProvider;
        }

        public GameChangedEto Map(Entities.GameOfTrust.Ef.GameOfTrust source)
        {   
            
            var depositToken = _tokenProvider.GetToken(source.DepositTokenId);
            var harvestToken = _tokenProvider.GetToken(source.HarvestTokenId);
            var gameChangedEto = _mapperProvider.Map<Entities.GameOfTrust.Ef.GameOfTrust, GameChangedEto>(source);
            gameChangedEto.DepositToken = depositToken;
            gameChangedEto.HarvestToken = harvestToken;
            return gameChangedEto;
        }

        public GameChangedEto Map(Entities.GameOfTrust.Ef.GameOfTrust source, GameChangedEto destination)
        {
            return Map(source);
        }
    }
}
