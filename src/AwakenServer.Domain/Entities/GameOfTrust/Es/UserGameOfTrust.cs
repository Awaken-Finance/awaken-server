using AElf.Indexing.Elasticsearch;

namespace AwakenServer.Entities.GameOfTrust.Es
{
    public class UserGameOfTrust: UserGameOfTrustBase,IIndexBuild
    {   
        public GameOfTrustWithToken GameOfTrust { get; set; }
    }
}