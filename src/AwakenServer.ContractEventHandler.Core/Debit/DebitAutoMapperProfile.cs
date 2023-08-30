using AutoMapper;
using AwakenServer.Debits.Entities.Ef;
using AwakenServer.ETOs.Debits;

namespace AwakenServer.ContractEventHandler.Debit
{
    public class DebitAutoMapperProfile: Profile
    {
        public DebitAutoMapperProfile()
        {
            /* You can configure your AutoMapper mapping configuration here.
             * Alternatively, you can split your mapping configurations
             * into multiple profile classes for a better organization. */

            // debit
            {
                CreateMap<CompController, CompControllerChangedEto>();
                CreateMap<CToken, CTokenChangedEto>();
                CreateMap<CTokenUserInfo, CTokenUserInfoChangedEto>();
                CreateMap<CTokenRecord, CTokenRecordChangedEto>();
            }
        }
    }
}