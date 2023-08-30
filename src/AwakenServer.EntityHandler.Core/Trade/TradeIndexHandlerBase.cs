using System;
using System.Threading.Tasks;
using AwakenServer.Tokens;
using AwakenServer.Trade;
using AwakenServer.Trade.Index;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.EntityHandler.Trade
{
    public abstract class TradeIndexHandlerBase : ITransientDependency
    {
        protected IObjectMapper ObjectMapper => LazyServiceProvider.LazyGetRequiredService<IObjectMapper>();
        protected TradePairAppService TradePairAppService => LazyServiceProvider.LazyGetRequiredService<TradePairAppService>();
        protected IDistributedEventBus DistributedEventBus => LazyServiceProvider.LazyGetRequiredService<IDistributedEventBus>();
        
        protected TokenAppService TokenAppService => LazyServiceProvider.LazyGetRequiredService<TokenAppService>();
        public IAbpLazyServiceProvider LazyServiceProvider { get; set; }
        
        protected async Task<Token> GetTokenAsync(Guid tokenId)
        {
            var tokenDto = await TokenAppService.GetAsync(tokenId);
            return ObjectMapper.Map<TokenDto, Token>(tokenDto);
        }

        protected async Task<TradePairWithToken> GetTradePariWithTokenAsync(Guid tradePairId)
        {
            var pairDto = await TradePairAppService.GetAsync(tradePairId);
            var pairWithToken = new TradePairWithToken();
            pairWithToken.Id = tradePairId;
            pairWithToken.Address = pairDto.Address;
            pairWithToken.FeeRate = pairDto.FeeRate;
            pairWithToken.IsTokenReversed = pairDto.IsTokenReversed;
            pairWithToken.ChainId = pairDto.ChainId;
            pairWithToken.Token0 = ObjectMapper.Map<TokenDto, Token>(pairDto.Token0);
            pairWithToken.Token1 = ObjectMapper.Map<TokenDto, Token>(pairDto.Token1);

            return pairWithToken;
        }
    }
}