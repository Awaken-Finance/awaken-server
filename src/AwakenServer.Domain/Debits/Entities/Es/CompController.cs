using System;
using AElf.Indexing.Elasticsearch;
using Nest;
using Token = AwakenServer.Tokens.Token;

namespace AwakenServer.Debits.Entities.Es
{
    public class CompController : EditableCompController, IIndexBuild
    {
        public CompController()
        {
            
        }
        
        public CompController(Guid id)
        {
            Id = id;
        }
        [Keyword] public override Guid Id { get; set; }
        public Token DividendToken { get; set; }
    }
}