using System.Collections.Generic;
using AwakenServer.Debits.Options;
using Volo.Abp.Modularity;

namespace AwakenServer.Debits
{
    [DependsOn(
        typeof(AwakenServerApplicationTestModule)
    )]
    public class DebitInterestModelTestModule: AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            Configure<InterestModelOption>(options =>
            {
                options.InterestModelList.Add(new InterestModelInfo
                {
                    ModelId = "1",
                    ModelName = "WhitePaper",
                    Parameters = new Dictionary<string, string>
                    {
                        {"MultiplierPerBlock", "10000"},
                        {"BaseRatePerBlock", "10000"},
                        {"Mantissa", "1000000000000000000"}
                    }
                });
                options.InterestModelList.Add(new InterestModelInfo
                {
                    ModelId = "2",
                    ModelName = "JumpRate",
                    Parameters = new Dictionary<string, string>
                    {
                        {"MultiplierPerBlock", "10000"},
                        {"BaseRatePerBlock", "10000"},
                        {"Kink", "500000000000000000"},
                        {"JumpMultiplierPerBlock", "10000"},
                        {"Mantissa", "1000000000000000000"}
                    }
                });
            });
            
            Configure<DebitOption>(options =>
            {
                options.DefaultModelId = "2";
                options.InterestModelMapList = new List<InterestModelMap>
                {
                    new InterestModelMap
                    {
                        InterestModelId = "1",
                        CTokenSymbolList = new List<string>{"TokenA"}
                    }
                };
            });
        }
    }
}