using AwakenServer.Dividend.Entities.Es;
using Volo.Abp.EventBus;

namespace AwakenServer.Dividend.ETOs
{
    [EventName("Dividend.DividendUserRecordChanged")]
    public class DividendUserRecordEto : DividendUserRecord
    {
    }
}