using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.Core.Enums;
using AElf.EthereumNode.EventHandler.TestBase;
using AwakenServer.ContractEventHandler.Debit.Ethereum.DTOs.CToken;
using Shouldly;
using Xunit;

namespace AwakenServer.Debits.Ethereum.Tests
{
    public partial class DebitEthereumAppServiceTests
    {
        [Fact(Skip = "no need")]
        public async Task Redeem_Without_Confirmed_Should_Not_Add_Records()
        {
            await MarketListAsync(CProjectToken.Address, CompController.ControllerAddress);
            await MarketEnteredAsync(CProjectToken.Address, DebitTestData.Gui);

            var mintAmount = "1000";
            var mintCToken = "2000";
            var minter = DebitTestData.Ming;
            await MintAsync(CProjectToken.Address, minter, mintAmount, mintCToken);
            
            var redeemTxHash = "redeem1";
            var redeemTimestamp = DateTime.Now;
            var redeemCTokens = mintCToken;
            await RedeemAsync(CProjectToken.Address, minter, mintAmount, redeemCTokens, redeemTxHash, redeemTimestamp,
                ContractEventStatus.Unconfirmed);
            var (_, cTokens) = await _esCTokenRepository.GetListAsync();
            var cToken =cTokens.Single(x => x.Id == CProjectToken.Id);;
            cToken.TotalCTokenMintAmount.ShouldBe(mintCToken);
            cToken.TotalUnderlyingAssetAmount.ShouldBe(mintAmount);
        }
        
        [Fact(Skip = "no need")]
        public async Task Redeem_With_Confirmed_Should_Modify_CToken_And_Add_Records()
        {
            await MarketListAsync(CProjectToken.Address, CompController.ControllerAddress);
            await MarketEnteredAsync(CProjectToken.Address, DebitTestData.Gui);

            var mintAmount = "1000";
            var mintCToken = "2000";
            var minter = DebitTestData.Ming;
            await MintAsync(CProjectToken.Address, minter, mintAmount, mintCToken);
            
            var redeemTxHash = "redeem1";
            var redeemTimestamp = DateTime.Now;
            var redeemCTokens = mintCToken;
            await RedeemAsync(CProjectToken.Address, minter, mintAmount, redeemCTokens, redeemTxHash, redeemTimestamp);
            var (_, cTokens) = await _esCTokenRepository.GetListAsync();
            var cToken =cTokens.Single(x => x.Id == CProjectToken.Id);;
            cToken.TotalCTokenMintAmount.ShouldBe(DebitTestData.ZeroBalance);
            cToken.TotalUnderlyingAssetAmount.ShouldBe(DebitTestData.ZeroBalance);
            
            var (recordCount, records) = await _esRecordRepository.GetListAsync();
            recordCount.ShouldBe(2);
            var targetRecord = records.First(x => x.BehaviorType == BehaviorType.Redeem);
            targetRecord.CTokenAmount.ShouldBe(mintCToken);
            targetRecord.UnderlyingTokenAmount.ShouldBe(mintAmount);
        }
        
        
        private async Task RedeemAsync(string cTokenAddress, string redeemer, string redeemAmount, string redeemTokens,
            string txHash, DateTime date, ContractEventStatus confirmStatus = ContractEventStatus.Confirmed)
        {
            var timestamp = DateTimeHelper.ToUnixTimeMilliseconds(date) / 1000;
            var redeemProcessor = GetRequiredService<IEventHandlerTestProcessor<Redeem>>();
            await redeemProcessor.HandleEventAsync(new Redeem
            {
                Redeemer = redeemer,
                RedeemAmount = BigInteger.Parse(redeemAmount),
                RedeemTokens = BigInteger.Parse(redeemTokens)
            }, GetDefaultEventContext(cTokenAddress, txHash, timestamp, confirmStatus));
        }
    }
}