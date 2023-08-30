using System;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.Chains
{
    public interface IChainRepository : IRepository<Chain, string>
    {
    }
}