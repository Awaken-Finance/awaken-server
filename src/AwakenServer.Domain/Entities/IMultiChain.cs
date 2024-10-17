using System;
using Orleans;

namespace AwakenServer.Entities
{
    public interface IMultiChain
    {
        string ChainId { get; set; }
    }
}