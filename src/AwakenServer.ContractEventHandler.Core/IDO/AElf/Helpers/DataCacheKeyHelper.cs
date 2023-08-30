using System;

namespace AwakenServer.ContractEventHandler.IDO.AElf.Helpers
{
    public static class DataCacheKeyHelper
    {
        public static string GetTokenKey(string chainId, string contractAddress, string symbol)
        {
            return $"{chainId}{contractAddress}{symbol}";
        }
        
        public static string GetPublicOfferingKey(string chainId, long id)
        {
            return $"{chainId}{id}";
        }
    }
}