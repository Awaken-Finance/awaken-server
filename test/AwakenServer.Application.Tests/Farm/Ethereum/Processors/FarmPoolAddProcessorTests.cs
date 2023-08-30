using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.BackgroundJob.DTO;
using AElf.EthereumNode.EventHandler.Core.Enums;
using AElf.EthereumNode.EventHandler.TestBase;
using AwakenServer.ContractEventHandler.Farm.Ethereum.DTOs;
using AwakenServer.Farm;
using Shouldly;
using Xunit;

namespace AwakenServer.Farms.Ethereum.Tests
{
    public partial class FarmAppServiceTests
    {
        [Fact(Skip = "no need")]
        public async Task Add_Pool_Should_Contain_Pool_Info()
        {
            var farmAddress = FarmTestData.MassiveFarmAddress;
            var pid = 0;
            var poolType = PoolType.Massive;
            var tokenAddress = FarmTestData.SwapTokenOneContractAddress;
            var lastRewardBlock = 1230;
            var weight = 0;
            await AddPoolAsync(farmAddress, pid, poolType, tokenAddress, lastRewardBlock, weight);
            var (_, esFarmPool) = await _esPoolRepository.GetListAsync();
            var targetPool = esFarmPool.First(x => x.FarmAddress == farmAddress && x.Pid == pid);
            targetPool.LastUpdateBlockHeight.ShouldBe(lastRewardBlock);
            targetPool.SwapToken.Address.ShouldBe(tokenAddress);
            targetPool.PoolType.ShouldBe(poolType);
            targetPool.Pid.ShouldBe(pid);
            targetPool.AccumulativeDividendProjectToken.ShouldBe(FarmTestData.ZeroBalance);
            targetPool.AccumulativeDividendUsdt.ShouldBe(FarmTestData.ZeroBalance);
            targetPool.TotalDepositAmount.ShouldBe(FarmTestData.ZeroBalance);
            targetPool.SwapToken.Id.ShouldNotBe(Guid.Empty);
            targetPool.Weight.ShouldBe(weight);
        }
        
        [Fact(Skip = "no need")]
        public async Task Add_GToken_Pool_Should_Contain_Pool_Info()
        {
            var farmAddress = FarmTestData.MassiveFarmAddress;
            var pid = 0;
            var poolType = PoolType.Massive;
            var tokenAddress = FarmTestData.SwapTokenThreeContractAddress;
            var lastRewardBlock = 1230;
            var weight = 0;
            await AddPoolAsync(farmAddress, pid, poolType, tokenAddress, lastRewardBlock, weight);
            var (_, esFarmPool) = await _esPoolRepository.GetListAsync();
            var targetPool = esFarmPool.First(x => x.FarmAddress == farmAddress && x.Pid == pid);
            targetPool.LastUpdateBlockHeight.ShouldBe(lastRewardBlock);
            targetPool.SwapToken.Address.ShouldBe(tokenAddress);
            targetPool.PoolType.ShouldBe(poolType);
            targetPool.Pid.ShouldBe(pid);
            targetPool.AccumulativeDividendProjectToken.ShouldBe(FarmTestData.ZeroBalance);
            targetPool.AccumulativeDividendUsdt.ShouldBe(FarmTestData.ZeroBalance);
            targetPool.TotalDepositAmount.ShouldBe(FarmTestData.ZeroBalance);
            targetPool.SwapToken.Id.ShouldNotBe(Guid.Empty);
            targetPool.Weight.ShouldBe(weight);
        }
        
        [Fact(Skip = "no need")]
        public async Task Add_Non_Tokens_Pool_Should_Contain_Pool_Info()
        {
            var farmAddress = FarmTestData.MassiveFarmAddress;
            var pid = 0;
            var poolType = PoolType.Massive;
            var tokenAddress = FarmTestData.SwapTokenFourContractAddress;
            var lastRewardBlock = 1230;
            var weight = 0;
            await AddPoolAsync(farmAddress, pid, poolType, tokenAddress, lastRewardBlock, weight);
            var (_, esFarmPool) = await _esPoolRepository.GetListAsync();
            var targetPool = esFarmPool.First(x => x.FarmAddress == farmAddress && x.Pid == pid);
            targetPool.LastUpdateBlockHeight.ShouldBe(lastRewardBlock);
            targetPool.SwapToken.Address.ShouldBe(tokenAddress);
            targetPool.PoolType.ShouldBe(poolType);
            targetPool.Pid.ShouldBe(pid);
            targetPool.AccumulativeDividendProjectToken.ShouldBe(FarmTestData.ZeroBalance);
            targetPool.AccumulativeDividendUsdt.ShouldBe(FarmTestData.ZeroBalance);
            targetPool.TotalDepositAmount.ShouldBe(FarmTestData.ZeroBalance);
            targetPool.SwapToken.Id.ShouldNotBe(Guid.Empty);
            targetPool.Weight.ShouldBe(weight);
        }

        
        [Fact(Skip = "no need")]
        public async Task Add_Pool_Should_Modify_Farm_Weight()
        {
            
            var farmAddress = FarmTestData.MassiveFarmAddress;
            var pid = 0;
            var poolType = PoolType.Massive;
            var tokenAddress = FarmTestData.SwapTokenOneContractAddress;
            var lastRewardBlock = 1230;
            var weight = 1001;
            await AddPoolAsync(farmAddress, pid, poolType, tokenAddress, lastRewardBlock, weight);
            var (farmCount, farms) = await _esFarmRepository.GetListAsync();
            farmCount.ShouldBe(1);
            var targetFarm = farms.First(x => x.FarmAddress == farmAddress);
            targetFarm.TotalWeight.ShouldBe(weight);
        }
        
        [Fact(Skip = "no need")]
        public async Task GeneralFarm_Pool_Update_Without_Confirmed_Should_Not_Modify_Pool()
        {
            var farmAddress = FarmTestData.MassiveFarmAddress;
            var pid = 0;
            var poolType = PoolType.Massive;
            var tokenAddress = FarmTestData.SwapTokenOneContractAddress;
            var lastRewardBlock = 1230;
            var weight = 1001;
            await AddPoolAsync(farmAddress, pid, poolType, tokenAddress, lastRewardBlock, weight, ContractEventStatus.Unconfirmed);
            var (farmCount, _) = await _esFarmRepository.GetListAsync();
            farmCount.ShouldBe(0);
        }
        
        
        private async Task AddPoolAsync(string farmAddress, int pid, PoolType poolType,
            string tokenAddress, long lastRewardBlock, int weight, ContractEventStatus confirmStatus = ContractEventStatus.Confirmed)
        {
            var poolAddedProcessor = GetRequiredService<IEventHandlerTestProcessor<PoolAdded>>();
            await poolAddedProcessor.HandleEventAsync(new PoolAdded
            {
                SwapToken = tokenAddress,
                LastRewardBlockHeight = lastRewardBlock,
                PoolType = (int) poolType,
                Pid = pid,
                AllocationPoint = weight,
            }, GetDefaultEventContext(farmAddress, confirmStatus: confirmStatus));
        }
        
        private ContractEventDetailsDto GetDefaultEventContext(string farmAddress, string txHash = null,
            long timestamp = 0, ContractEventStatus confirmStatus = ContractEventStatus.Confirmed)
        {
            return new ContractEventDetailsDto
            {
                StatusEnum = confirmStatus,
                NodeName = FarmTestData.DefaultNodeName,
                Address = farmAddress,
                TransactionHash = txHash,
                Timestamp = timestamp
            };
        }
    }
}