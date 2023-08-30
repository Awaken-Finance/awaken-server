using AElf.Indexing.Elasticsearch;

namespace AwakenServer.Entities.GameOfTrust.Es
{
    public class GameOfTrust: GameOfTrustWithToken,IIndexBuild
    {   
        public string TotalValueLocked { get; set; }
        public string FineAmount { get; set; }
    }
}