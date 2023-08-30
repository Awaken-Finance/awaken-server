using AutoMapper;
using AwakenServer.Entities.GameOfTrust.Ef;
using AwakenServer.ETOs.GameOfTrust;

namespace EthereumEventHandler
{
    public class EthereumEventHandlerAutoMapperProfile: Profile
    {
        public EthereumEventHandlerAutoMapperProfile()
        {
            /* You can configure your AutoMapper mapping configuration here.
             * Alternatively, you can split your mapping configurations
             * into multiple profile classes for a better organization. */
            
            CreateMap<GameOfTrust,GameChangedEto>();
            CreateMap<GameOfTrustRecord, GameOfTrustRecordCreatedEto>();
            CreateMap<UserGameOfTrust, UserGameOfTrustChangedEto>();
        }
    }
}