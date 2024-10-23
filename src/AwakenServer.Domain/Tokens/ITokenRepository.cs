using System;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.Tokens
{
    public interface ITokenRepository : IRepository<TokenEntity, Guid>
    {
        
    }
}