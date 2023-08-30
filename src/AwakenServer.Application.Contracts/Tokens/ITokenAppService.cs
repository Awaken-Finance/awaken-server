using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace AwakenServer.Tokens
{
    public interface ITokenAppService: IApplicationService
    {
        Task<TokenDto> GetAsync(Guid id);
        
        Task<TokenDto> GetAsync(GetTokenInput input);
        
        Task<TokenDto> CreateAsync(TokenCreateDto input);
        
        void DeleteAsync(Guid id);

        TokenDto GetBySymbolCache(string symbol);
    }
}