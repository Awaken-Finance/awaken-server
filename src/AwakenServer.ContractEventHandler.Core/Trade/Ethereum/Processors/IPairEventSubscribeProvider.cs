using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.BackgroundJob.Helpers;
using AElf.EthereumNode.EventHandler.BackgroundJob.Providers;
using AElf.EthereumNode.EventHandler.Core.Domains.Entities;
using AElf.EthereumNode.EventHandler.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AwakenServer.ContractEventHandler.Trade.Ethereum.Processors
{
    public interface IPairEventSubscribeProvider
    {
        Task SubscribeEventAsync(string nodeName, long startBlockNumber, string pariAddress);
    }

    public class PairEventSubscribeProvider : IPairEventSubscribeProvider, ITransientDependency
    {
        private readonly EthereumBackgroundJobOption _ethereumBackgroundJobOption;
        private readonly IServiceProvider _serviceProvider;
        private readonly ApiOptions _apiOptions;

        public PairEventSubscribeProvider(IOptionsSnapshot<EthereumBackgroundJobOption> ethereumBackgroundJobOption,
            IServiceProvider serviceProvider, IOptionsSnapshot<ApiOptions> apiOptions)
        {
            _serviceProvider = serviceProvider;
            _ethereumBackgroundJobOption = ethereumBackgroundJobOption.Value;
            _apiOptions = apiOptions.Value;
        }

        public async Task SubscribeEventAsync(string nodeName, long startBlockNumber, string pariAddress)
        {
            var eventFilters = new EventFilterList
            {
                Filters = new List<EventFilter>(),
                StartBlock = startBlockNumber
            };
            eventFilters.Filters.Add(await GetSyncEventFilterAsync(nodeName, pariAddress));
            eventFilters.Filters.Add(await GetMintEventFilterAsync(nodeName,pariAddress));
            eventFilters.Filters.Add(await GetBurnEventFilterAsync(nodeName,pariAddress));
            eventFilters.Filters.Add(await GetSwapEventFilterAsync(nodeName,pariAddress));

            await AddEventFilterHelper.AddEventFiltersToEventeumDynamicallyAsync(_apiOptions.EventeumApi, eventFilters);
        }

        private async Task<EventFilter> GetSyncEventFilterAsync(string nodeName, string contractAddress)
        {
            var eventName = "Sync";
            var id = contractAddress + eventName;
            var eventFilter = new EventFilter
            {
                Id = id,
                ContractAddress = contractAddress,
                Node = nodeName,
                EventSpecification = new ContractEventSpecification
                {
                    EventName = eventName,
                    NonIndexedParameterDefinitions = new List<ParameterDefinition>
                    {
                        new()
                        {
                            Position = 0,
                            Type = "UINT112"
                        },
                        new()
                        {
                            Position = 1,
                            Type = "UINT112"
                        }
                    }
                },
                QueueName = _ethereumBackgroundJobOption.QueueName,
                // RouterKey = "EventeumMessage",
                // Exchange = _abpRabbitMqEventBusOptions.ExchangeName,
            };
            var provider = _serviceProvider.GetService<IEthereumEventProcessorProvider>();
            await provider.AddProcessorsAsync(new List<EthereumProcessorKey>
            {
                new EthereumProcessorKey
                {
                    FilterId = id,
                    ContractAddress = contractAddress,
                    EventName = eventName,
                    NodeName = nodeName,
                    ProcessorName = nameof(SyncEventProcessor)
                }
            });

            return eventFilter;
        }

        private async Task<EventFilter> GetMintEventFilterAsync(string nodeName, string contractAddress)
        {
            var eventName = "Mint";
            var id = contractAddress + eventName;
            var eventFilter = new EventFilter
            {
                Id = id,
                ContractAddress = contractAddress,
                Node = nodeName,
                EventSpecification = new ContractEventSpecification
                {
                    EventName = eventName,
                    NonIndexedParameterDefinitions = new List<ParameterDefinition>
                    {
                        new()
                        {
                            Position = 0,
                            Type = "ADDRESS",
                        },
                        new()
                        {
                            Position = 1,
                            Type = "UINT256"
                        },
                        new()
                        {
                            Position = 2,
                            Type = "UINT256"
                        },
                        new()
                        {
                            Position = 3,
                            Type = "ADDRESS"
                        },
                        new()
                        {
                            Position = 4,
                            Type = "UINT256"
                        },
                        new()
                        {
                            Position = 5,
                            Type = "STRING"
                        }
                    }
                },
                QueueName = _ethereumBackgroundJobOption.QueueName,
                // RouterKey = "EventeumMessage",
                // Exchange = _abpRabbitMqEventBusOptions.ExchangeName,
            };
            var provider = _serviceProvider.GetService<IEthereumEventProcessorProvider>();
            await provider.AddProcessorsAsync(new List<EthereumProcessorKey>
            {
                new EthereumProcessorKey
                {
                    FilterId = id,
                    ContractAddress = contractAddress,
                    EventName = eventName,
                    NodeName = nodeName,
                    ProcessorName = nameof(MintEventProcessor)
                }
            });

            return eventFilter;
        }

        private async Task<EventFilter> GetBurnEventFilterAsync(string nodeName, string contractAddress)
        {
            var eventName = "Burn";
            var id = contractAddress + eventName;
            var eventFilter = new EventFilter
            {
                Id = id,
                ContractAddress = contractAddress,
                Node = nodeName,
                EventSpecification = new ContractEventSpecification
                {
                    EventName = eventName,
                    NonIndexedParameterDefinitions = new List<ParameterDefinition>
                    {
                        new()
                        {
                            Position = 0,
                            Type = "ADDRESS",
                        },
                        new()
                        {
                            Position = 1,
                            Type = "UINT256"
                        },
                        new()
                        {
                            Position = 2,
                            Type = "UINT256"
                        },
                        new()
                        {
                            Position = 3,
                            Type = "ADDRESS"
                        },
                        new()
                        {
                            Position = 4,
                            Type = "UINT256"
                        }
                    }
                },
                QueueName = _ethereumBackgroundJobOption.QueueName,
                // RouterKey = "EventeumMessage",
                // Exchange = _abpRabbitMqEventBusOptions.ExchangeName,
            };
            var provider = _serviceProvider.GetService<IEthereumEventProcessorProvider>();
            await provider.AddProcessorsAsync(new List<EthereumProcessorKey>
            {
                new EthereumProcessorKey
                {
                    FilterId = id,
                    ContractAddress = contractAddress,
                    EventName = eventName,
                    NodeName = nodeName,
                    ProcessorName = nameof(BurnEventProcessor)
                }
            });

            return eventFilter;
        }

        private async Task<EventFilter> GetSwapEventFilterAsync(string nodeName, string contractAddress)
        {
            var eventName = "Swap";
            var id = contractAddress + eventName;
            var eventFilter = new EventFilter
            {
                Id = id,
                ContractAddress = contractAddress,
                Node = nodeName,
                EventSpecification = new ContractEventSpecification
                {
                    EventName = eventName,
                    NonIndexedParameterDefinitions = new List<ParameterDefinition>
                    {
                        new()
                        {
                            Position = 0,
                            Type = "ADDRESS",
                        },
                        new()
                        {
                            Position = 1,
                            Type = "UINT256"
                        },
                        new()
                        {
                            Position = 2,
                            Type = "UINT256"
                        },
                        new()
                        {
                            Position = 3,
                            Type = "UINT256"
                        },
                        new()
                        {
                            Position = 4,
                            Type = "UINT256"
                        },
                        new()
                        {
                            Position = 5,
                            Type = "ADDRESS"
                        },
                        new()
                        {
                            Position = 6,
                            Type = "STRING"
                        }
                    }
                },
                QueueName = _ethereumBackgroundJobOption.QueueName,
                // RouterKey = "EventeumMessage",
                // Exchange = _abpRabbitMqEventBusOptions.ExchangeName,
            };
            var provider = _serviceProvider.GetService<IEthereumEventProcessorProvider>();
            await provider.AddProcessorsAsync(new List<EthereumProcessorKey>
            {
                new EthereumProcessorKey
                {
                    FilterId = id,
                    ContractAddress = contractAddress,
                    EventName = eventName,
                    NodeName = nodeName,
                    ProcessorName = nameof(SwapEventProcessor)
                }
            });

            return eventFilter;
        }
    }
}