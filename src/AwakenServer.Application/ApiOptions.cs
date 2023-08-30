using System.Collections.Generic;

namespace AwakenServer
{
    public class ApiOptions
    {
        public string EventeumApi { get; set; }
        public Dictionary<string,string> ChainNodeApis { get; set; }
    }
} 