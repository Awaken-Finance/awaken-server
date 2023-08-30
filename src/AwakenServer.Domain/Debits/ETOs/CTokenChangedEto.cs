using AwakenServer.Debits.Entities.Es;
using Volo.Abp.EventBus;

namespace AwakenServer.ETOs.Debits
{
    [EventName("Debit.CToken")]
    public class CTokenChangedEto : CToken
    {
    }
}