using System;
using System.Threading.Tasks;
using AwakenServer.Tokens;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.EntityHandler.Price
{
    public class PriceIndexHandlerBase :ITransientDependency
    {
        public IAbpLazyServiceProvider LazyServiceProvider { get; set; }
        protected IObjectMapper ObjectMapper => LazyServiceProvider.LazyGetRequiredService<IObjectMapper>();
        private TokenAppService _tokenAppService => LazyServiceProvider.LazyGetRequiredService<TokenAppService>();
        
        protected async Task<Token> GetTokenAsync(Guid tokenId)
        {
            var tokenDto = await _tokenAppService.GetAsync(tokenId);
            return ObjectMapper.Map<TokenDto, Token>(tokenDto);
        }
    }
}