using AutoMapper;
using AwakenServer.ETOs.Farms;
using AwakenServer.Farms.Entities.Ef;

namespace AwakenServer.ContractEventHandler.Farm
{
    public class EthereumEventHandlerAutoMapperProfile: Profile
    {
        public EthereumEventHandlerAutoMapperProfile()
        {
            /* You can configure your AutoMapper mapping configuration here.
             * Alternatively, you can split your mapping configurations
             * into multiple profile classes for a better organization. */
            
            CreateMap<Farms.Entities.Ef.Farm, FarmChangedEto>();
            CreateMap<FarmPool, FarmPoolChangedEto>();
            CreateMap<FarmUserInfo, FarmUserInfoChangedEto>();
            CreateMap<FarmRecord, FarmRecordChangedEto>();
        }
    }
}