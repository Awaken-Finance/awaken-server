using AElf.Indexing.Elasticsearch;

namespace AwakenServer.IDO.Entities.Es
{
    public class UserPublicOffering : UserPublicOfferingBase, IIndexBuild
    {
        public PublicOfferingWithToken PublicOfferingInfo { get; set; }
    }
}