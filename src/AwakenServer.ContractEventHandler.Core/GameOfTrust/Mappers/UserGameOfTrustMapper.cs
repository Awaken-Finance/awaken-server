using AwakenServer.ContractEventHandler.Providers;
using AwakenServer.Entities.GameOfTrust.Ef;
using AwakenServer.ETOs.GameOfTrust;
using AwakenServer.Tokens;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.ContractEventHandler.GameOfTrust.Mappers
{
    public class UserGameOfTrustMapper :IObjectMapper<UserGameOfTrust,UserGameOfTrustChangedEto>,ITransientDependency
    {
        
        private readonly ICachedDataProvider<Entities.GameOfTrust.Ef.GameOfTrust> _gameInfoProvider;
        private readonly IAutoObjectMappingProvider _mapperProvider;
        private readonly ITokenProvider _tokenProvider;

        public UserGameOfTrustMapper(ICachedDataProvider<Entities.GameOfTrust.Ef.GameOfTrust> gameInfoProvider, ITokenProvider tokenProvider, IAutoObjectMappingProvider mapperProvider)
        {
            _gameInfoProvider = gameInfoProvider;
            _tokenProvider = tokenProvider;
            _mapperProvider = mapperProvider;
        }

        public UserGameOfTrustChangedEto Map(UserGameOfTrust source)
        {
            var gameOfTrust = _gameInfoProvider.GetCachedDataById(source.GameOfTrustId);
            var depositToken = _tokenProvider.GetToken(gameOfTrust.DepositTokenId);
            var harvestToken = _tokenProvider.GetToken(gameOfTrust.HarvestTokenId);

            var userGameOfTrustChangedEto = _mapperProvider.Map<UserGameOfTrust, UserGameOfTrustChangedEto>(source);
            userGameOfTrustChangedEto.GameOfTrust = _mapperProvider.Map<Entities.GameOfTrust.Ef.GameOfTrust, GameChangedEto>(gameOfTrust);
            userGameOfTrustChangedEto.GameOfTrust.DepositToken = depositToken;
            userGameOfTrustChangedEto.GameOfTrust.HarvestToken = harvestToken;
            return userGameOfTrustChangedEto;
        }

        public UserGameOfTrustChangedEto Map(UserGameOfTrust source, UserGameOfTrustChangedEto destination)
        {
            return Map(source);
        }
    }
}