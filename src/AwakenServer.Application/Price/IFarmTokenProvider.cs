using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AwakenServer.Price
{
    public interface IFarmTokenProvider
    {
        public FarmToken GetFarmToken(string chainName, string address);
    }

    public class FarmTokenProvider : IFarmTokenProvider, ISingletonDependency
    {
        private readonly FarmTokenOptions _farmTokenOptions;

        public FarmTokenProvider(IOptionsSnapshot<FarmTokenOptions> farmTokenOptions)
        {
            _farmTokenOptions = farmTokenOptions.Value;
        }

        public FarmToken GetFarmToken(string chainName, string address)
        {
            if (_farmTokenOptions.FarmTokens.TryGetValue($"{chainName}-{address}", out var farmToken))
            {
                return farmToken;
            }

            throw new KeyNotFoundException("Invalid Farm token");
        }
    }
    
    
}