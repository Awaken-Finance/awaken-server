using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.TestBase;
using AwakenServer.Constants;
using AwakenServer.ContractEventHandler.GameOfTrust.Dtos;
using Shouldly;
using Xunit;

namespace AwakenServer.Applications.GameOfTrust
{
    public partial class GameOfTrustAppServiceTests
    {
        [Fact(Skip = "no need")]
        public async Task Up_To_Standard_Event_Test()
        {
            await initPool();
            var pid = 0;
            var blocks = 6700;
            var currentMarketcap = 3999999999;
            await UpToStandardAsync(
                GameOfTrustTestData.ContractAddress,
                pid,
                blocks,
                currentMarketcap);
            var (_, esPoolList) = await _esGameRepository.GetListAsync();
            var targetPool = esPoolList.First(x => x.Address == GameOfTrustTestData.ContractAddress && x.Pid == pid);
            targetPool.Pid.ShouldBe(pid);
            targetPool.UnlockHeight.ShouldBe(blocks);
        }

        private async Task UpToStandardAsync(string contractAddress, int pid, long blocks, BigInteger currentMarketcap)
        {
            var upToStandardProcessor = GetRequiredService<IEventHandlerTestProcessor<UpToStandardEventDto>>();
            await upToStandardProcessor.HandleEventAsync(
                new UpToStandardEventDto
                {
                    Pid = pid,
                    Blocks = blocks,
                    CurrentMarketcap = currentMarketcap
                },
                GetDefaultEventContext(contractAddress));
        }
    }
}