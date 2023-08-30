using System;
using System.Collections.Generic;
using AwakenServer.Debits.Providers.InterestModel;
using Volo.Abp.DependencyInjection;

namespace AwakenServer.Debits.Providers
{
    public interface IInterestModelFactory
    {
        IInterestModel CreateInterestModelByName(string modelName, Dictionary<string, string> parameters = null);
    }

    public class InterestModelFactory : IInterestModelFactory, ITransientDependency
    {
        public IInterestModel CreateInterestModelByName(string modelName, Dictionary<string, string> parameters = null)
        {
            return @modelName switch
            {
                WhitePaperInterestRateModel.InterestModelName => new WhitePaperInterestRateModel(parameters),
                JumpRateInterestModel.InterestModelName => new JumpRateInterestModel(parameters),
                _ => throw new Exception($"Invalid interest model name: {modelName}")
            };
        }
    }
}