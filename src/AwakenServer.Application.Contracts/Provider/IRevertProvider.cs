using System.Collections.Generic;
using System.Threading.Tasks;
using AwakenServer.Common;

namespace AwakenServer.Provider;

public interface IRevertProvider
{
    Task CheckOrAddUnconfirmedTransaction(long currentConfirmedHeight, EventType type, string chainId, long blockHeight, string transactionHash);
    
    Task<List<string>> GetNeedDeleteTransactionsAsync(EventType eventType, string chainId);
}