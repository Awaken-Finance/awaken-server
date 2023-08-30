using System;
using AElf.Indexing.Elasticsearch;
using Nest;
using Token = AwakenServer.Tokens.Token;

namespace AwakenServer.Farms.Entities.Es
{
    public class FarmPool : EditableStateFarmPool, IIndexBuild
    {
        public FarmPool()
        {
            
        }
        
        public FarmPool(Guid id)
        {
            Id = id;
        }
        [Keyword] public override Guid Id { get; set; }
        [Keyword] public string FarmAddress { get; set; }
        public Token SwapToken { get; set; }
        public Token Token1 { get; set; }
        public Token Token2 { get; set; }
    }
}