using AutoMapper;
using AwakenServer.Dividend.Entities.Ef;
using AwakenServer.Dividend.ETOs;

namespace AwakenServer.ContractEventHandler.Dividend
{
    public class DividendAutoMapperProfile : Profile
    {
        public DividendAutoMapperProfile()
        {
            /* You can configure your AutoMapper mapping configuration here.
             * Alternatively, you can split your mapping configurations
             * into multiple profile classes for a better organization. */
            
            CreateMap<AwakenServer.Dividend.Entities.Dividend, DividendEto>();
            CreateMap<DividendPool, DividendPoolEto>();
            CreateMap<DividendPoolToken, DividendPoolTokenEto>();
            CreateMap<DividendToken, DividendTokenEto>();
            CreateMap<DividendUserRecord, DividendUserRecordEto>();
            CreateMap<DividendUserToken, DividendUserTokenEto>();
            CreateMap<DividendUserPool, DividendUserPoolEto>();
        }
    }
}