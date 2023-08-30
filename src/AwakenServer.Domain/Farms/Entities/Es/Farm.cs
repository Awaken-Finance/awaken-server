using System;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace AwakenServer.Farms.Entities.Es
{
    public class Farm : EditableStateFarm, IIndexBuild
    {
        public Farm()
        {
        }

        public Farm(Guid id)
        {
            Id = id;
        }

        [Keyword] public override Guid Id { get; set; }
    }
}