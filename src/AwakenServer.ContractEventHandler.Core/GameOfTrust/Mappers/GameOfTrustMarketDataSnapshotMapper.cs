using AwakenServer.Entities.GameOfTrust.Ef;
using AwakenServer.ETOs.GameOfTrust;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

// todo  automapper
namespace AwakenServer.ContractEventHandler.GameOfTrust.Mappers
{
    public class GameOfTrustMarketDataMapper : IObjectMapper<GameOfTrustMarketData,GameOfTrustMarketDataSnapshotEto>,ITransientDependency
    {
        public GameOfTrustMarketDataSnapshotEto Map(GameOfTrustMarketData source)
        {
            return new GameOfTrustMarketDataSnapshotEto(source.Id)
            {
                Price = source.Price,
                Timestamp = source.Timestamp,
                ChainId = source.ChainId,
                MarketCap = source.MarketCap,
                TotalSupply = source.TotalSupply
            };
        }

        public GameOfTrustMarketDataSnapshotEto Map(GameOfTrustMarketData source, GameOfTrustMarketDataSnapshotEto destination)
        {
            return Map(source);
        }
    }
}