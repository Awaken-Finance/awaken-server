using System;
using System.Collections.Generic;
using System.Linq;
using AwakenServer.Debits.DebitAppDto;
using AwakenServer.Debits.Options;
using AwakenServer.Debits.Providers.InterestModel;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AwakenServer.Debits.Providers
{
    public interface IInterestModelProvider
    {
        IInterestModel GetInterestModel(CTokenDto cToken);
    }

    public class InterestModelProvider : IInterestModelProvider, ISingletonDependency
    {
        private readonly Dictionary<string, IInterestModel> _interestModelDic;
        private readonly IInterestModel _defaultModel;

        public InterestModelProvider(IInterestModelFactory interestModelFactory,
            IOptionsSnapshot<DebitOption> debitOption, IOptionsSnapshot<InterestModelOption> interestModelOption)
        {
            var defaultModelId = debitOption.Value.DefaultModelId;
            var defaultModelConfig =
                interestModelOption.Value.InterestModelList.FirstOrDefault(x => x.ModelId == defaultModelId);
            _defaultModel = defaultModelConfig != null
                ? interestModelFactory.CreateInterestModelByName(defaultModelConfig.ModelName, defaultModelConfig.Parameters)
                : interestModelFactory.CreateInterestModelByName(JumpRateInterestModel.InterestModelName);

            _interestModelDic = new Dictionary<string, IInterestModel>();
            var tokenInterestModelMapList = debitOption.Value.InterestModelMapList;
            if (tokenInterestModelMapList == null || !tokenInterestModelMapList.Any())
            {
                return;
            }

            var allInterestModelIdDic = interestModelOption.Value.InterestModelList.ToDictionary(x => x.ModelId,
                x => interestModelFactory.CreateInterestModelByName(x.ModelName, x.Parameters));
            tokenInterestModelMapList.ForEach(c =>
            {
                if (!allInterestModelIdDic.TryGetValue(c.InterestModelId, out var interestModel))
                {
                    throw new Exception($"Lack of ModelId : {c.InterestModelId}");
                }
                
                c.CTokenSymbolList.ForEach(cToken => { _interestModelDic.TryAdd(cToken, interestModel); });
            });
        }

        public IInterestModel GetInterestModel(CTokenDto cToken)
        {
            var interestModel = _interestModelDic.GetValueOrDefault(GetCTokenKey(cToken));
            return interestModel ?? _defaultModel;
        }

        private string GetCTokenKey(CTokenDto cToken)
        {
            return cToken.Symbol;
        }
    }
}