using System;
using System.Collections.Generic;
using AwakenServer.Debits.Entities.Es;
using AwakenServer.Tokens;

namespace AwakenServer.Debits.Options
{
    public class DebitOption
    {
        public bool IsResetData { get; set; }
        public List<CompControllerConfig> CompControllerList { get; set; }
        public List<InterestModelMap> InterestModelMapList { get; set; }
        public string DefaultModelId { get; set; }
        public string MainToken { get; set; } = "AWKN";
    }

    public class InterestModelMap
    {
        public string InterestModelId { get; set; }
        public List<string> CTokenSymbolList { get; set; }
    }

    public class CompControllerConfig
    {
        public Guid Id { get; set; }
        public string ChainId { get; set; }
        public string ControllerAddress { get; set; }
        public string CloseFactorMantissa { get; set; }
        public Guid CompTokenId { get; set; }
        public string CompTokenAddress { get; set; }
        public string CompTokenSymbol { get; set; }
        public int CompTokenDecimals { get; set; }
        
        public CompController GetCompController()
        {
            return new (Id)
            {

                ChainId = ChainId,
                ControllerAddress = ControllerAddress,
                CloseFactorMantissa = CloseFactorMantissa,
                DividendToken = new Token(CompTokenId)
                {
                    Address = CompTokenAddress,
                    ChainId = ChainId,
                    Symbol = CompTokenSymbol,
                    Decimals = CompTokenDecimals
                }
            };
        }
    }
}