using System.Collections.Generic;

namespace AwakenServer.Debits.Options
{
    public class InterestModelOption
    {
        public List<InterestModelInfo> InterestModelList { get; set; } = new List<InterestModelInfo>();
    }
    
    public class InterestModelInfo
    {
        public string ModelId { get; set; }
        public string ModelName { get; set; }
        public Dictionary<string, string> Parameters { get; set; }
    }
}