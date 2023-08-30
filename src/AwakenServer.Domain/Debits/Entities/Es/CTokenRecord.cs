using System;
using AElf.Indexing.Elasticsearch;
using Nest;
using Token = AwakenServer.Tokens.Token;

namespace AwakenServer.Debits.Entities.Es
{
    public class CTokenRecord : CTokenRecordBase, IIndexBuild
    {
        [Keyword] public override Guid Id { get; set; }
        public double CTokenDecimalAmount { get; set; }
        public double UnderlyingTokenDecimalAmount { get; set; }
        public CTokenBase CToken { get; set; }
        public CompControllerBase CompControllerInfo { get; set; }
        public Token UnderlyingAssetToken { get; set; }
    }
}