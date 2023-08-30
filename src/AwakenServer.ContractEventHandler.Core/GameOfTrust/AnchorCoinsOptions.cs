using System.Collections.Generic;

namespace AwakenServer.ContractEventHandler
{
    public class AnchorCoin
    {
        public int Decimal { get; set; }
        public string Symbol { get; set; }
        public string Chain { get; set; }
    }

    public class AnchorCoinsOptions
    {
        public List<AnchorCoin> AnchorCoinsList { get; set; }

        public AnchorCoinsOptions()
        {
        }

        public AnchorCoinsOptions(List<AnchorCoin> anchorCoinsList)
        {
            AnchorCoinsList = anchorCoinsList;
        }
    }
}