using System;
using System.Collections.Concurrent;
using Volo.Abp.DependencyInjection;
using System.Collections.Generic;

namespace AwakenServer.Hubs
{
    public interface ITradeHubConnectionProvider
    {
        void AddUserConnection(string chainId, Guid tradePairId, string address, string connectionId);
        void AddRemovedUserConnection(string chainId, Guid tradePairId, string address, string connectionId);
        List<string> GetUserConnectionList(string chainId, Guid tradePairId, string address);
        List<string> GetRemovedUserConnectionList(string chainId, Guid tradePairId, string address);
        void ClearUserConnection(string chainId, Guid tradePairId, string address, long timestamp, string connectionId);
        void ClearRemovedUserConnection(string chainId, Guid tradePairId, string address, string connectionId);
        void ClearUserConnection(string connectionId);
        void ClearRemovedUserConnection(string connectionId);
    }

    public class TradeHubConnectionProvider : ITradeHubConnectionProvider, ISingletonDependency
    {
        private readonly ConcurrentDictionary<string, List<string>> _userConnections = new();
        private readonly ConcurrentDictionary<string, string> _userConnectionIds = new();

        public void AddUserConnection(string chainId, Guid tradePairId, string address,
            string connectionId)
        {
            var key = GetUserConnectionKey(chainId, tradePairId, address);
            if (_userConnectionIds.TryAdd($"{connectionId}-0", key))
            {
                _userConnections.TryGetValue(key, out var value);
                if (value == null)
                {
                    value = new List<string>();
                    _userConnections.AddOrUpdate(key, value,
                        (key, value) =>
                        {
                            value.Add(connectionId);
                            return value;
                        });
                }
                else
                {
                    value.Add(connectionId);
                }
            }
        }

        public void AddRemovedUserConnection(string chainId, Guid tradePairId, string address, string connectionId)
        {
            var key = GetRemovedUserConnectionKey(chainId, tradePairId, address);
            if (_userConnectionIds.TryAdd(connectionId, key))
            {
                _userConnections.TryGetValue(key, out var value);
                if (value == null)
                {
                    value = new List<string>();
                    _userConnections.AddOrUpdate(key, value,
                        (key, value) =>
                        {
                            value.Add(connectionId);
                            return value;
                        });
                }
                else
                {
                    value.Add(connectionId);
                }
            }
        }

        public List<string> GetUserConnectionList(string chainId, Guid tradePairId, string address)
        {
            var key = GetUserConnectionKey(chainId, tradePairId, address);
            _userConnections.TryGetValue(key, out var value);
            return value;
        }

        public List<string> GetRemovedUserConnectionList(string chainId, Guid tradePairId, string address)
        {
            var key = GetUserConnectionKey(chainId, tradePairId, address);
            _userConnections.TryGetValue(key, out var value);
            return value;
        }

        public void ClearUserConnection(string chainId, Guid tradePairId, string address, long timestamp,
            string connectionId)
        {
            var key = GetUserConnectionKey(chainId, tradePairId, address);
            _userConnections.TryGetValue(key, out var value);
            value?.Remove(connectionId);
            _userConnectionIds.TryRemove($"{connectionId}-0", out _);
        }

        public void ClearRemovedUserConnection(string chainId, Guid tradePairId, string address, string connectionId)
        {
            var key = GetRemovedUserConnectionKey(chainId, tradePairId, address);
            _userConnections.TryGetValue(key, out var value);
            value?.Remove(connectionId);
            _userConnectionIds.TryRemove(connectionId, out _);
        }

        public void ClearUserConnection(string connectionId)
        {
            if (_userConnectionIds.TryRemove($"{connectionId}-0", out var key))
            {
                _userConnections.TryGetValue(key, out var value);
                value?.Remove(connectionId);
            }
        }

        public void ClearRemovedUserConnection(string connectionId)
        {
            if (_userConnectionIds.TryRemove(connectionId, out var key))
            {
                _userConnections.TryRemove(key, out _);
            }
        }

        private string GetUserConnectionKey(string chainId, Guid tradePairId, string address)
        {
            return $"{chainId}{tradePairId}{address}-UserConnection";
        }

        private string GetRemovedUserConnectionKey(string chainId, Guid tradePairId, string address)
        {
            return $"{chainId}{tradePairId}{address}-removedUserConnection";
        }
    }
}