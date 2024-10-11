using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp.DependencyInjection;

namespace AwakenServer.Hubs
{
    public interface ITradeHubGroupProvider
    {
        string GetTradeRecordGroupName(string chainId, Guid tradePairId, long timestamp);
        string GetRemovedTradeRecordGroupName(string chainId, Guid tradePairId);
        List<string> GetAllTradeRecordGroup();
        
        string GetKlineGroupName(string chainId, Guid tradePairId, int period);
        List<string> GetAllKlineGroup();
        
        string GetActivityRankingListGroupName(int activityId);
        List<string> GetAllActivityRankingListGroup();

        string GetTradePairGroupName(string chainId);
        
        string GetTradePairDetailName(string tradePairId);
        List<string> GetAllTradePairGroup();
    }
    
    public class TradeHubGroupProvider : ITradeHubGroupProvider, ISingletonDependency
    {
        private readonly ConcurrentDictionary<string, string> _tradeRecordGroups = new();
        private readonly ConcurrentDictionary<string, int> _klineGroups = new();
        private readonly ConcurrentDictionary<string, string> _tradePairGroups = new();
        private readonly ConcurrentDictionary<string, int> _activityRankingListGroups = new();
        
        public string GetTradeRecordGroupName(string chainId, Guid tradePairId, long timestamp)
        {
            var groupName = $"TradeRecord{chainId}{tradePairId}-{timestamp}";
            _tradeRecordGroups.TryAdd(groupName, string.Empty);
            return groupName;
        }
        
        public string GetRemovedTradeRecordGroupName(string chainId, Guid tradePairId)
        {
            var groupName = $"RemovedTradeRecord{chainId}{tradePairId}";
            _tradeRecordGroups.TryAdd(groupName, string.Empty);
            return groupName;
        }

        public List<string> GetAllTradeRecordGroup()
        {
            return _tradeRecordGroups.Keys.ToList();
        }

        public string GetKlineGroupName(string chainId, Guid tradePairId, int period)
        {
            var groupName = $"Kline{chainId}{tradePairId}{period}";
            _klineGroups.TryAdd(groupName, period);
            return groupName;
        }

        public List<string> GetAllKlineGroup()
        {
            return _klineGroups.Keys.ToList();
        }
        
        public string GetTradePairGroupName(string chainId)
        {
            var groupName = $"TradePair{chainId}";
            _tradePairGroups.TryAdd(groupName, string.Empty);
            return groupName;
        }

        public string GetTradePairDetailName(string tradePairId)
        {
            var groupName = $"TradePairDetail{tradePairId}";
            _tradePairGroups.TryAdd(groupName, string.Empty);
            return groupName;
        }
        public List<string> GetAllTradePairGroup()
        {
            return _tradePairGroups.Keys.ToList();
        }

        public string GetActivityRankingListGroupName(int activityId)
        {
            var groupName = $"ActivityRankingList{activityId}";
            _activityRankingListGroups.TryAdd(groupName, activityId);
            return groupName;
        }

        public List<string> GetAllActivityRankingListGroup()
        {
            return _activityRankingListGroups.Keys.ToList();
        }
    }
}