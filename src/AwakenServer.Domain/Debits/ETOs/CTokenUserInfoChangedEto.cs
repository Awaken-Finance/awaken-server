using AwakenServer.Debits.Entities.Es;
using Volo.Abp.EventBus;

namespace AwakenServer.ETOs.Debits
{
    [EventName("Debit.CTokenUserInfo")]
    public class CTokenUserInfoChangedEto : CTokenUserInfo
    {
    }
}