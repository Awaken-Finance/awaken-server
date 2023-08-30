using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Constants;
using AwakenServer.Entities.GameOfTrust.Es;
using AwakenServer.GameOfTrust;
using AwakenServer.GameOfTrust.DTos;
using AwakenServer.GameOfTrust.DTos.Input;
using AwakenServer.Price;
using AwakenServer.Price.Dtos;
using AwakenServer.Processors;
using AwakenServer.Trade.Dtos;
using Nethereum.Util;
using Shouldly;
using Xunit;

namespace AwakenServer.Applications.GameOfTrust
{
    public partial class GameOfTrustAppServiceTests : GameOfTrustApplicationTestBase
    {
        private readonly INESTReaderRepository<Entities.GameOfTrust.Es.GameOfTrust, Guid> _esGameRepository;
        private readonly INESTRepository<GameOfTrustMarketData, Guid> _esMarketRepository;
        private readonly INESTRepository<UserGameOfTrust, Guid> _esUserRepository;
        private readonly INESTRepository<GameOfTrustRecord, Guid> _esUserRecordRepository;
        private readonly IGameOfTrustService _gameService;
        private readonly IPriceAppService _priceAppService;

        public GameOfTrustAppServiceTests()
        {
            _esMarketRepository = GetRequiredService<INESTRepository<GameOfTrustMarketData, Guid>>();
            _esGameRepository = GetRequiredService<INESTReaderRepository<Entities.GameOfTrust.Es.GameOfTrust, Guid>>();
            _esUserRepository = GetRequiredService<INESTRepository<UserGameOfTrust, Guid>>();
            _esUserRecordRepository = GetRequiredService<INESTRepository<GameOfTrustRecord, Guid>>();
            _gameService = GetRequiredService<IGameOfTrustService>();
            _priceAppService = GetRequiredService<IPriceAppService>();
        }


        [Fact(Skip = "no need")]
        public async Task GetGameList_Should_Contain_Test()
        {
            await initPool();
            await initPool_Sashimi();
            var gameDto = await _gameService.GetGameOfTrustsAsync(new GetGameListInput
            {
                SkipCount = 0,
                MaxResultCount = 10,
                ChainId = chainEth.Id
            });
            gameDto.TotalCount.ShouldBe(2);
            var gameList = gameDto.Items;
            var game = gameList.First(x => x.Pid == 0);
            game.ChainId.ShouldBe(chainEth.Id);
            game.EndHeight.ShouldBe(GameOfTrustTestData.EndHeight);
            game.FineAmount.ShouldBe(GameOfTrustTestData.FineAmount.ToString());
            game.RewardRate.ShouldBe(GameOfTrustTestData.RewardRate.ToString());
            game.StartHeight.ShouldBe(GameOfTrustTestData.StartHeight);
            game.DepositToken.Address.ShouldBe(tokenA.Address);
            game.HarvestToken.Address.ShouldBe(tokenB.Address);
            game.UnlockCycle.ShouldBe(GameOfTrustTestData.UnlockCycle);
            game.TotalAmountLimit.ShouldBe(GameOfTrustTestData.TotalAmountLimit.ToString());
            game.TotalValueLocked.ShouldBe(GameOfTrustTestData.TotalValueLocked.ToString());
            game.UnlockMarketCap.ShouldBe(GameOfTrustTestData.UnlockMarketCap.ToString());
        }

        [Fact(Skip = "no need")]
        public async Task GetGame_By_id_Should_Success_Test()
        {
            await initPool_Sashimi();
            var gameOfTrustDto = await _gameService.GetAsync(game1.Id);
            gameOfTrustDto.Id.ShouldBe(game1.Id);
            gameOfTrustDto.ChainId.ShouldBe(game1.ChainId);
            gameOfTrustDto.DepositToken.Id.ShouldBe(game1.DepositTokenId);
            gameOfTrustDto.HarvestToken.Id.ShouldBe(game1.HarvestTokenId);
            gameOfTrustDto.Pid.ShouldBe(game1.Pid);
            gameOfTrustDto.Address.ShouldBe(game1.Address);
        }

        [Fact(Skip = "no need")]
        public async Task GetMarkedDatas_Should_Contain_Test()
        {
            await initPool();
            var contractAddress = GameOfTrustTestData.ContractAddress;
            await CurrentMarketCapAsync(contractAddress, 400000, 400000 * 2000, 2000);
            await CurrentMarketCapAsync(contractAddress, 500000, 500000 * 2500, 2500);
            await CurrentMarketCapAsync(contractAddress, 500000, 500000 * 3000, 3000);
            await CurrentMarketCapAsync(contractAddress, 550000, 550000 * 3500, 3500);
            var marketDatasAsync = await _gameService.GetMarketDatasAsync(new GetMarketDataInput
            {
                ChainId = chainEth.Id,
                SkipCount = 0,
                MaxResultCount = 10,
            });
            marketDatasAsync.TotalCount.ShouldBe(4);
            var marketDataDtos = marketDatasAsync.Items;
            marketDataDtos.First().Price
                .ShouldBe((BigDecimal.Parse("2000") / BigInteger.Pow(10, tokenUSD.Decimals)).ToString());
            marketDataDtos.First().TotalSupply
                .ShouldBe(((BigDecimal) 400000 / BigInteger.Pow(10, tokenB.Decimals)).ToString());
            marketDataDtos.First().MarketCap
                .ShouldBe(((BigDecimal) (400000 * 2000) / BigInteger.Pow(10, tokenUSD.Decimals)).ToString());
            marketDataDtos[0].Timestamp.ShouldBeLessThanOrEqualTo(marketDataDtos[1].Timestamp);
            marketDataDtos[1].Timestamp.ShouldBeLessThanOrEqualTo(marketDataDtos[2].Timestamp);
            marketDataDtos[2].Timestamp.ShouldBeLessThanOrEqualTo(marketDataDtos[3].Timestamp);
        }


        [Fact(Skip = "no need")]
        public async Task GetUserGameOfTrust_Should_Contain_Test()
        {
            var contractAddressTokenA = GameOfTrustTestData.ContractAddress;
            var contractAddressTokenB = GameOfTrustTestData.ContractAddressSashimi;
            var sender1 = GameOfTrustTestData.ADDRESS_USER1;
            var sender2 = GameOfTrustTestData.ADDRESS_USER2;

            await initPool_ProjectToken(0);
            await initPool_ProjectToken(1);
            await initPool_ProjectToken(2);
            await initPool_ProjectToken(3);
            await initPool_ProjectToken(4);

            await initPool_Sashimi(0);
            await initPool_Sashimi(1);
            await initPool_Sashimi(2);
            await initPool_Sashimi(3);
            
            await DepositAsync(contractAddressTokenB, 0, sender1, 500);
            await DepositAsync(contractAddressTokenB, 1, sender1, 13000);
            await DepositAsync(contractAddressTokenB, 2, sender1, 14000);

            await DepositAsync(contractAddressTokenA, 0, sender1, 5000);
            await DepositAsync(contractAddressTokenA, 1, sender1, 1000);
            await DepositAsync(contractAddressTokenA, 3, sender1, 10000);
            await DepositAsync(contractAddressTokenA, 1, sender2, 20000);
            await DepositAsync(contractAddressTokenA, 3, sender2, 6000);
            await DepositAsync(contractAddressTokenA, 4, sender2, 15000);
            var userGameOfTrustsAsync = await _gameService.GetUserGameOfTrustsAsync(new GetUserGameOfTrustsInput
            {
                Address = sender1,
                ChainId = chainEth.Id,
                SkipCount = 0,
                MaxResultCount = 10,
            });
            userGameOfTrustsAsync.TotalCount.ShouldBe(6);
            var userGameofTrustDtos = userGameOfTrustsAsync.Items;
            foreach (var userGameofTrustDto in userGameofTrustDtos)
            {
                if (userGameofTrustDto.GameOfTrust.Address == contractAddressTokenB)
                {
                    userGameofTrustDto.GameOfTrust.DepositToken.Symbol.ShouldBe(tokenA.Symbol);
                }
                else
                {
                    userGameofTrustDto.GameOfTrust.DepositToken.Symbol.ShouldBe(tokenB.Symbol);
                }
            }

            userGameOfTrustsAsync = await _gameService.GetUserGameOfTrustsAsync(new GetUserGameOfTrustsInput
            {
                Address = sender1,
                ChainId = chainEth.Id,
                SkipCount = 0,
                MaxResultCount = 10,
                DepositTokenSymbol = tokenB.Symbol,
                HarvestTokenSymbol = tokenB.Symbol
            });
            userGameOfTrustsAsync.TotalCount.ShouldBe(3);
            userGameofTrustDtos = userGameOfTrustsAsync.Items;
            foreach (var userGameofTrustDto in userGameofTrustDtos)
            {
                userGameofTrustDto.Address.ShouldBe(sender1);
                userGameofTrustDto.GameOfTrust.DepositToken.Symbol.ShouldBe(tokenB.Symbol);
                userGameofTrustDto.GameOfTrust.HarvestToken.Symbol.ShouldBe(tokenB.Symbol);
                userGameofTrustDto.GameOfTrust.Address.ShouldBe(contractAddressTokenA);
            }

            userGameOfTrustsAsync = await _gameService.GetUserGameOfTrustsAsync(new GetUserGameOfTrustsInput
            {
                Address = sender2,
                ChainId = chainEth.Id,
                SkipCount = 0,
                MaxResultCount = 10,
                DepositTokenSymbol = tokenB.Symbol,
                HarvestTokenSymbol = tokenB.Symbol
            });
            userGameOfTrustsAsync.TotalCount.ShouldBe(3);

            userGameOfTrustsAsync = await _gameService.GetUserGameOfTrustsAsync(new GetUserGameOfTrustsInput
            {
                Address = sender1,
                ChainId = chainEth.Id,
                DepositTokenSymbol = tokenA.Symbol,
                HarvestTokenSymbol = tokenB.Symbol,
                SkipCount = 0,
                MaxResultCount = 10
            });

            userGameOfTrustsAsync.TotalCount.ShouldBe(3);
            var game = userGameOfTrustsAsync.Items.First();
            game.Address.ShouldBe(sender1);
            game.GameOfTrust.Address.ShouldBe(contractAddressTokenB);
        }

        [Fact(Skip = "no need")]
        public async Task GetMarketcaps_Should_Contain_Test()
        {
            await initPool_ProjectToken_unlockMarkedCap(0, "30000");
            await initPool_ProjectToken_unlockMarkedCap(1, "40000");
            await initPool_ProjectToken_unlockMarkedCap(2, "50000");
            await initPool_ProjectToken_unlockMarkedCap(3, "60000");
            await initPool_ProjectToken_unlockMarkedCap(4, "700000");

            var marketCapsAsync = await _gameService.GetMarketCapsAsync(new GetMarketCapsInput
            {
                ChainId = chainEth.Id,
                DepositTokenSymbol = tokenA.Symbol,
                HarvestTokenSymbol = tokenB.Symbol
            });
            marketCapsAsync.Items.Count.ShouldBe(0);

            marketCapsAsync = await _gameService.GetMarketCapsAsync(new GetMarketCapsInput
            {
                ChainId = chainEth.Id,
                DepositTokenSymbol = tokenB.Symbol,
                HarvestTokenSymbol = tokenB.Symbol
            });
            marketCapsAsync.Items.Count.ShouldBe(5);
            foreach (var marketCapsDto in marketCapsAsync.Items)
            {
                switch (marketCapsDto.Pid)
                {
                    case 0:
                        marketCapsDto.UnlockMarketCap.ShouldBe("30000");
                        break;
                    case 1:
                        marketCapsDto.UnlockMarketCap.ShouldBe("40000");
                        break;
                    case 2:
                        marketCapsDto.UnlockMarketCap.ShouldBe("50000");
                        break;
                    case 3:
                        marketCapsDto.UnlockMarketCap.ShouldBe("60000");
                        break;
                    default:
                        marketCapsDto.UnlockMarketCap.ShouldBe("700000");
                        break;
                }
            }
        }

        [Fact(Skip = "no need")]
        public async Task GetUserGameOfTrustRecord_Should_Contain_Test()
        {
            var contractAddress = GameOfTrustTestData.ContractAddress;
            var sender = GameOfTrustTestData.ADDRESS_USER1;
            await Withdraw_Sashimi_Unlocked_Not_Finshed_Should_Success_Test();
            var userGameOfTrustRecord = await _gameService.GetUserGameOfTrustRecord(new GetUserGameOfTrustRecordInput
            {
                ChainId = chainEth.Id,
                Address = sender,
                SkipCount = 0,
                MaxResultCount = 10
            });
            userGameOfTrustRecord.TotalCount.ShouldBe(3);
            userGameOfTrustRecord = await _gameService.GetUserGameOfTrustRecord(new GetUserGameOfTrustRecordInput
            {
                ChainId = chainEth.Id,
                Address = sender,
                SkipCount = 0,
                MaxResultCount = 10,
                type = BehaviorType.Deposit
            });

            userGameOfTrustRecord.TotalCount.ShouldBe(1);
            var time = userGameOfTrustRecord.Items.First().Timestamp;

            userGameOfTrustRecord = await _gameService.GetUserGameOfTrustRecord(new GetUserGameOfTrustRecordInput
            {
                ChainId = chainEth.Id,
                Address = sender,
                SkipCount = 0,
                MaxResultCount = 10,
                type = BehaviorType.Withdraw
            });
            userGameOfTrustRecord.TotalCount.ShouldBe(1);
            userGameOfTrustRecord = await _gameService.GetUserGameOfTrustRecord(new GetUserGameOfTrustRecordInput
            {
                ChainId = chainEth.Id,
                Address = sender,
                SkipCount = 0,
                MaxResultCount = 10,
                TimestampMin = time + 10 * 1000
            });
            userGameOfTrustRecord.TotalCount.ShouldBe(0);

            userGameOfTrustRecord = await _gameService.GetUserGameOfTrustRecord(new GetUserGameOfTrustRecordInput
            {
                ChainId = chainEth.Id,
                Address = sender,
                SkipCount = 0,
                MaxResultCount = 10,
                TimestampMin = time - 2 * 1000
            });
            userGameOfTrustRecord.TotalCount.ShouldBe(3);

            userGameOfTrustRecord = await _gameService.GetUserGameOfTrustRecord(new GetUserGameOfTrustRecordInput
            {
                ChainId = chainEth.Id,
                Address = sender,
                SkipCount = 0,
                MaxResultCount = 10,
                TimestampMax = time + 1000 * 1000
            });
            userGameOfTrustRecord.TotalCount.ShouldBe(3);


            userGameOfTrustRecord = await _gameService.GetUserGameOfTrustRecord(new GetUserGameOfTrustRecordInput
            {
                ChainId = chainEth.Id,
                Address = sender,
                SkipCount = 0,
                MaxResultCount = 10,
                TimestampMax = time - 1000 * 1000
            });
            userGameOfTrustRecord.TotalCount.ShouldBe(0);
        }

        [Fact(Skip = "no need")]
        public async Task GetUserAsset_Should_Correct_Test()
        {
            var sender = GameOfTrustTestData.ADDRESS_USER1;
            await Withdraw_Sashimi_StarkePeriod_Should_Success_Test();
            var lastSashimi = 5;
            var userAssert = await _gameService.GetUserAssertAsync(new GetUserAssertInput
            {
                Address = sender,
                ChainId = chainEth.Id
            });
            var sashimiPrice = await _priceAppService.GetTokenPriceAsync(new GetTokenPriceInput
            {
                Symbol = "SASHIMI"
            });
            var usedPrice = await _priceAppService.GetTokenPriceAsync(new GetTokenPriceInput
            {
                Symbol = "USDT"
            });
            var btcPrirce = await _priceAppService.GetTokenPriceAsync(new GetTokenPriceInput
            {
                Symbol = "BTC"
            });

            var usd = double.Parse(lastSashimi.ToString()) * double.Parse(sashimiPrice.ToString());
            var btc = usd/double.Parse(btcPrirce.ToString());
            userAssert.AssetBTC.ShouldBe(btc);
            userAssert.AssetUSD.ShouldBe(usd);
        }
    }
}