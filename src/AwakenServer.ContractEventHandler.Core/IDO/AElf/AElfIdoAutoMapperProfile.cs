using AutoMapper;
using AwakenServer.IDO.Entities;
using AwakenServer.IDO.Entities.Ef;
using AwakenServer.IDO.ETOs;

namespace AwakenServer.ContractEventHandler.IDO.AElf
{
    public class AElfIdoAutoMapperProfile : Profile
    {
        public AElfIdoAutoMapperProfile()
        {
            /* You can configure your AutoMapper mapping configuration here.
           * Alternatively, you can split your mapping configurations
           * into multiple profile classes for a better organization. */
            CreateMap<PublicOffering, PublicOfferingEto>();
            CreateMap<UserPublicOffering, UserPublicOfferingEto>();
            CreateMap<PublicOfferingRecord, PublicOfferingRecordEto>();
            CreateMap<PublicOffering, PublicOfferingWithToken>();
        }
    }
}