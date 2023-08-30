using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.TestBase;
using AwakenServer.Constants;
using AwakenServer.ContractEventHandler.GameOfTrust.Dtos;
using AwakenServer.GameOfTrust;
using Nethereum.Util;
using Shouldly;
using Xunit;

namespace AwakenServer.Applications.GameOfTrust
{
    public partial class GameOfTrustAppServiceTests
    {   
        [Fact(Skip = "no need")]
        public async Task Deposit_Should_Success_Test()
        {
            await initPool();
            var contractAddress = GameOfTrustTestData.ContractAddress;
            var pid = 0;
            var sender = GameOfTrustTestData.ADDRESS_USER1;
            var amount = BigInteger.Pow(10, 18) * 20;

            var finalAmount = (BigDecimal) amount / BigInteger.Pow(10, tokenA.Decimals);
            
            await DepositAsync(
                contractAddress,
                pid,
                sender,
                amount);
            var (_, esPoolList) = await _esGameRepository.GetListAsync();
            var targetPool = esPoolList.First(x => x.Address == contractAddress && x.Pid == pid);
            var (_,esUserList) = await _esUserRepository.GetListAsync();
            var targetUser = esUserList.First(x =>
                x.Address == sender && x.GameOfTrust.Address == contractAddress && x.GameOfTrust.Pid == pid);
            targetPool.TotalValueLocked.ShouldBe(finalAmount.ToString());
            targetUser.ValueLocked.ShouldBe(finalAmount.ToString());
            targetUser.Address.ShouldBe(sender);
            targetUser.ReceivedFineAmount.ShouldBe(BigInteger.Zero.ToString());
            targetUser.GameOfTrust.Address.ShouldBe(contractAddress);
            var (_,esUserRecordList)=await _esUserRecordRepository.GetListAsync();
            var userRecord =
                esUserRecordList.First(x => x.GameOfTrust.Address == contractAddress && x.Address == sender);
            userRecord.Amount.ShouldBe(finalAmount.ToString());
            userRecord.Type.ShouldBe(BehaviorType.Deposit);
        }

        private async Task DepositAsync(string contractAddress, int pid, string sender, BigInteger amount)
        {
            var depositProcessor = GetRequiredService<IEventHandlerTestProcessor<DepositEventDto>>();
            await depositProcessor.HandleEventAsync(new DepositEventDto
            {
                Pid = pid,
                Amount = amount,
                Sender = sender
                
            },GetDefaultEventContext(contractAddress));
            
        }
    }
}