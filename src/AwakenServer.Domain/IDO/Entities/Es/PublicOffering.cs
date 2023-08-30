
using AElf.Indexing.Elasticsearch;

namespace AwakenServer.IDO.Entities.Es
{
    public class PublicOffering : PublicOfferingWithToken, IIndexBuild
    {
        public long CurrentAmount { get; set; }
        public long RaiseCurrentAmount { get; set; }
    }
}