using System;

namespace AwakenServer
{
    public class TestEnvironmentProvider
    {
        public string EthChainId { get; set; }
        public string EthChainName { get; set; }
        public string AElfChainId { get; set; }
        public string AElfChainName { get; set; }
        public Guid TradePairEthUsdtId { get; set; }
        public string TradePairEthUsdtAddress { get; set; }
        public Guid TradePairBtcEthId { get; set; }
        public string TradePairBtcEthAddress { get; set; }
        public Guid TradePariElfUsdtId { get; set; }
        public Guid TokenUsdtId { get; set; }
        public string TokenUsdtSymbol { get; set; }
        public Guid TokenEthId { get; set; }
        public string TokenEthSymbol { get; set; }
        public Guid TokenBtcId { get; set; }
        public string TokenBtcSymbol { get; set; }
    }
}