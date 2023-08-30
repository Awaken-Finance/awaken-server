using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AwakenServer.ContractEventHandler.Trade
{
    public interface ITradePairTokenOrderProvider
    {
        int GetTokenWeight(string tokenAddress, string tokenSymbol);
    }
    
    public class TradePairTokenOrderProvider:ITradePairTokenOrderProvider,ISingletonDependency
    {
        private readonly TradePairTokenOrderOptions _tradePairTokenOrderOptions;

        private readonly Dictionary<string, TradePairToken> _tradePairTokenCache;

        public TradePairTokenOrderProvider(IOptionsSnapshot<TradePairTokenOrderOptions> tradePairTokenOrderOptions)
        {
            _tradePairTokenOrderOptions = tradePairTokenOrderOptions.Value;

            _tradePairTokenCache = new Dictionary<string, TradePairToken>();
            foreach (var token in _tradePairTokenOrderOptions.TradePairTokens)
            {
                _tradePairTokenCache[token.Address+token.Symbol] = token;
            }
        }

        public int GetTokenWeight(string tokenAddress, string tokenSymbol)
        {
            if (_tradePairTokenCache.TryGetValue(tokenAddress + tokenSymbol, out var token))
            {
                return token.Weight;
            }

            return 0;
        }
    }
}