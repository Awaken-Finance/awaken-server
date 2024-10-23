using System;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Entities;
using JetBrains.Annotations;
using Nest;
using Orleans;

namespace AwakenServer.Tokens
{
    [GenerateSerializer]
    public class Token
    {
        [Id(0)] [Keyword] public Guid Id { get; set; }
        [Id(1)] [Keyword] public virtual string ChainId { get; set; }
        [Id(2)] [Keyword] [NotNull] public virtual string Address { get; set; }
        [Id(3)] [Keyword] [NotNull] public virtual string Symbol { get; set; }
        [Id(4)] public virtual int Decimals { get; set; }
        [Id(5)] [Keyword] public string ImageUri { get; set; }
        public Token()
        {
        }

        public Token(Guid id)
        {
            Id = id;
        }
    }
}