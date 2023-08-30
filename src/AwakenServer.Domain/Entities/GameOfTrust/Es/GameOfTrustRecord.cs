using AElf.Indexing.Elasticsearch;

namespace AwakenServer.Entities.GameOfTrust.Es
{
    public class GameOfTrustRecord: GameOfTrustRecordBase,IIndexBuild
    {   
        public GameOfTrustWithToken GameOfTrust { get; set; }
        
    }
}