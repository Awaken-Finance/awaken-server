using System;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Entities;
using JetBrains.Annotations;
using Nest;

namespace AwakenServer.Tokens
{
    public class Token : MultiChainEntity<Guid>, IIndexBuild
    {
        [Keyword] public override Guid Id { get; set; }

        [Keyword] [NotNull] public virtual string Address { get; set; }

        [Keyword] [NotNull] public virtual string Symbol { get; set; }

        public virtual int Decimals { get; set; }
        [Keyword] public string ImageUri { get; set; }
        public Token()
        {
        }

        public Token(Guid id)
            : base(id)
        {
        }
    }
}