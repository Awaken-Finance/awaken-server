using AElf.AElfNode.EventHandler.BackgroundJob.ETO;
using AElf.AElfNode.EventHandler.BackgroundJob.Services;
using Volo.Abp.DependencyInjection;

namespace AwakenServer.ContractEventHandler
{
    public class AElfTransactionFilter: ITransactionFilter, ITransientDependency
    {
        public bool IsValidTransaction(TransactionResultEto txEto)
        {
            return txEto.Status == "MINED";
        }
    }
}