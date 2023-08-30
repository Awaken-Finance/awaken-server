using AwakenServer.ContractEventHandler.Providers;
using AwakenServer.Entities.GameOfTrust.Ef;
using AwakenServer.ETOs.GameOfTrust;
using AwakenServer.Tokens;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.ContractEventHandler.GameOfTrust.Mappers
{
    public class GameOfTrustRecordMapper : IObjectMapper<GameOfTrustRecord, GameOfTrustRecordCreatedEto>,
        ITransientDependency
    {
        private readonly ICachedDataProvider<Entities.GameOfTrust.Ef.GameOfTrust> _gameInfoProvider;
        private readonly IAutoObjectMappingProvider _mapperProvider;
        private readonly ITokenProvider _tokenProvider;

        public GameOfTrustRecordMapper(ITokenProvider tokenProvider, ICachedDataProvider<Entities.GameOfTrust.Ef.GameOfTrust> gameInfoProvider, IAutoObjectMappingProvider mapperProvider)
        {
            _tokenProvider = tokenProvider;
            _gameInfoProvider = gameInfoProvider;
            _mapperProvider = mapperProvider;
        }

        public GameOfTrustRecordCreatedEto Map(GameOfTrustRecord source)
        {
            var gameOfTrust = _gameInfoProvider.GetCachedDataById(source.GameOfTrustId);
            var depositToken = _tokenProvider.GetToken(gameOfTrust.DepositTokenId);
            var harvestToken = _tokenProvider.GetToken(gameOfTrust.HarvestTokenId);
            var gameOfTrustRecordCreatedEto = _mapperProvider.Map<GameOfTrustRecord, GameOfTrustRecordCreatedEto>(source);
            gameOfTrustRecordCreatedEto.GameOfTrust = _mapperProvider.Map<Entities.GameOfTrust.Ef.GameOfTrust,GameChangedEto>(gameOfTrust);
            gameOfTrustRecordCreatedEto.GameOfTrust.DepositToken = depositToken;
            gameOfTrustRecordCreatedEto.GameOfTrust.HarvestToken = harvestToken;
            return gameOfTrustRecordCreatedEto;
        }

        public GameOfTrustRecordCreatedEto Map(GameOfTrustRecord source, GameOfTrustRecordCreatedEto destination)
        {
            return Map(source);
        }
    }
}