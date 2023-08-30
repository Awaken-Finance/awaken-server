using AElf.Indexing.Elasticsearch;

namespace AwakenServer.IDO.Entities.Es
{
    public class PublicOfferingRecord : PublicOfferingRecordBase, IIndexBuild
    {
        public PublicOfferingWithToken PublicOfferingInfo { get; set; }
    }
}