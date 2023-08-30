using System;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.Price
{
    public interface IOtherLpTokenRepository  : IRepository<OtherLpToken, Guid>
    {
        
    }
}