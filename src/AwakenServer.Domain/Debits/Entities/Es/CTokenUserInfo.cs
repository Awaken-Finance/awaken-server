using System;
using AElf.Indexing.Elasticsearch;
using Nest;
using Token = AwakenServer.Tokens.Token;


namespace AwakenServer.Debits.Entities.Es
{
    public class CTokenUserInfo: CTokenUserInfoBase, IIndexBuild
    {
        [Keyword] public override Guid Id { get; set; }
        public Token UnderlyingToken { get; set; }
        public CTokenBase CTokenInfo { get; set; }
        public CompControllerBase CompInfo { get; set;}
    }
}