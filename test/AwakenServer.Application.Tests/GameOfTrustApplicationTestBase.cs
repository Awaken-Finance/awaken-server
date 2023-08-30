using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using AwakenServer.Applications.GameOfTrust;
using AwakenServer.Chains;
using AwakenServer.Constants;
using AwakenServer.Tokens;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Threading;

namespace AwakenServer.Processors
{
    public abstract class GameOfTrustApplicationTestBase : AwakenServerTestBase<GameOfTrustTestModule>
    {
        protected Dictionary<string, Token> TokenInfo { get; }
        protected TokenDto tokenA;
        protected TokenDto tokenB;
        protected TokenDto tokenUSD;
        protected ChainDto chainEth;
        protected static string ChainEthId = "ETH";
        protected readonly IRepository<Entities.GameOfTrust.Ef.GameOfTrust> _gameRepository;
        protected readonly IRepository<Entities.GameOfTrust.Ef.UserGameOfTrust> _userRepository;
        protected readonly IRepository<Entities.GameOfTrust.Ef.GameOfTrustRecord> _userRecordRepository;
        protected Entities.GameOfTrust.Ef.GameOfTrust game0;
        protected Entities.GameOfTrust.Ef.GameOfTrust game1;
        
        protected GameOfTrustApplicationTestBase()
        {
            TokenInfo = new Dictionary<string, Token>();
            TokenInfo.TryAdd(GameOfTrustTestData.DepositTokenSymbol01,
                new Token(GameOfTrustTestData.DepositTokenSymbol01, "0xeF4623aa86e96Be9a5b5C718BA72704C2Ef14BAb", 18));
            TokenInfo.TryAdd(GameOfTrustTestData.DepositTokenSymbol02,
                new Token(GameOfTrustTestData.DepositTokenSymbol02, "0x2fcdE7c234173DA68E201Db1645a1c6A340C2983", 18));
            TokenInfo.TryAdd(GameOfTrustTestData.HarvestTokenSymbol,
                new Token(GameOfTrustTestData.HarvestTokenSymbol, "0x2fcdE7c234173DA68E201Db1645a1c6A340C2983", 18));
            TokenInfo.TryAdd(GameOfTrustTestData.AnchorTokenSymbol,
                new Token(GameOfTrustTestData.AnchorTokenSymbol, "", 6));
            var chainService = GetRequiredService<IChainAppService>();
            var tokenService = GetRequiredService<ITokenAppService>();


            _gameRepository = GetRequiredService<IRepository<Entities.GameOfTrust.Ef.GameOfTrust>>();
            _userRepository = GetRequiredService<IRepository<Entities.GameOfTrust.Ef.UserGameOfTrust>>();
            _userRecordRepository = GetRequiredService<IRepository<Entities.GameOfTrust.Ef.GameOfTrustRecord>>();


            chainEth = AsyncHelper.RunSync(async () => await chainService.CreateAsync(new ChainCreateDto
            {
                Id = ChainEthId,
                Name = "Ethereum"
            }));

            tokenA = AsyncHelper.RunSync(async () => await tokenService.CreateAsync(new TokenCreateDto
            {
                Address = "0xeF4623aa86e96Be9a5b5C718BA72704C2Ef14BAb",
                Decimals = 18,
                Symbol = "SASHIMI",
                ChainId = chainEth.Id
            }));
            
            tokenB = AsyncHelper.RunSync(async () => await tokenService.CreateAsync(new TokenCreateDto
            {
                Address = "0x2fcdE7c234173DA68E201Db1645a1c6A340C2982",
                Decimals = 18,
                Symbol = "ISTAR",
                ChainId = chainEth.Id
            }));
           
            tokenUSD = AsyncHelper.RunSync(async () => await tokenService.CreateAsync(new TokenCreateDto
            {
                Address = "0x2fcde7c234173da68e201db1645a1c6a340c2981",
                Decimals = 6,
                Symbol = "USDT",
                ChainId = chainEth.Id
            }));
        }

        /**
         * init pool-sashimi
         */
        public async Task initPool()
        {
            game0 = await _gameRepository.InsertAsync(new Entities.GameOfTrust.Ef.GameOfTrust
            {
                Address = GameOfTrustTestData.ContractAddress,
                Pid = 0,
                BlocksDaily = 8749,
                ChainId = chainEth.Id,
                EndHeight = GameOfTrustTestData.EndHeight,
                FineAmount = GameOfTrustTestData.FineAmount.ToString(),
                RewardRate = GameOfTrustTestData.RewardRate.ToString(),
                StartHeight = GameOfTrustTestData.StartHeight,
                UnlockCycle = GameOfTrustTestData.UnlockCycle,
                UnlockHeight = 0,
                DepositTokenId = tokenA.Id,
                HarvestTokenId = tokenB.Id,
                TotalAmountLimit = GameOfTrustTestData.TotalAmountLimit.ToString(),
                TotalValueLocked = GameOfTrustTestData.TotalValueLocked.ToString(),
                UnlockMarketCap = GameOfTrustTestData.UnlockMarketCap.ToString()
            });
        }

        public async Task initPool_Sashimi()
        {
            game1 = await _gameRepository.InsertAsync(new Entities.GameOfTrust.Ef.GameOfTrust
            {
                Address = GameOfTrustTestData.ContractAddress,
                Pid = 1,
                BlocksDaily = 1000,
                ChainId = chainEth.Id,
                EndHeight = 20000,
                FineAmount = 0.ToString(),
                RewardRate = GameOfTrustTestData.RewardRate.ToString(),
                StartHeight = 10000,
                UnlockCycle = 10000,
                UnlockHeight = 0,
                DepositTokenId = tokenA.Id,
                HarvestTokenId = tokenB.Id,
                TotalAmountLimit = (BigInteger.Pow(10, tokenA.Decimals) * 20).ToString(),
                TotalValueLocked = 0.ToString(),
                UnlockMarketCap = (BigInteger.Pow(10, tokenA.Decimals) * 40).ToString()
            });
        }
        
        public async Task initPool_Sashimi(int pid)
        {
            game1 = await _gameRepository.InsertAsync(new Entities.GameOfTrust.Ef.GameOfTrust
            {
                Address = GameOfTrustTestData.ContractAddressSashimi,
                Pid = pid,
                BlocksDaily = 1000,
                ChainId = chainEth.Id,
                EndHeight = 20000,
                FineAmount = 0.ToString(),
                RewardRate = GameOfTrustTestData.RewardRate.ToString(),
                StartHeight = 10000,
                UnlockCycle = 10000,
                UnlockHeight = 0,
                DepositTokenId = tokenA.Id,
                HarvestTokenId = tokenB.Id,
                TotalAmountLimit = (BigInteger.Pow(10, tokenA.Decimals) * 20).ToString(),
                TotalValueLocked = 0.ToString(),
                UnlockMarketCap = (BigInteger.Pow(10, tokenA.Decimals) * 40).ToString()
            });
        }
        
        public async Task initPool_ProjectToken(int pid)
        {
            game1 = await _gameRepository.InsertAsync(new Entities.GameOfTrust.Ef.GameOfTrust
            {
                Address = GameOfTrustTestData.ContractAddress,
                Pid = pid,
                BlocksDaily = 1000,
                ChainId = chainEth.Id,
                EndHeight = 20000,
                FineAmount = 0.ToString(),
                RewardRate = GameOfTrustTestData.RewardRate.ToString(),
                StartHeight = 10000,
                UnlockCycle = 10000,
                UnlockHeight = 0,
                DepositTokenId = tokenB.Id,
                HarvestTokenId = tokenB.Id,
                TotalAmountLimit = (BigInteger.Pow(10, tokenB.Decimals) * 20).ToString(),
                TotalValueLocked = 0.ToString(),
                UnlockMarketCap = (BigInteger.Pow(10, tokenB.Decimals) * 40).ToString()
            });
        }
        
        public async Task initPool_ProjectToken_unlockMarkedCap(int pid,string unlockMarketCap)
        {
            game1 = await _gameRepository.InsertAsync(new Entities.GameOfTrust.Ef.GameOfTrust
            {
                Address = GameOfTrustTestData.ContractAddress,
                Pid = pid,
                BlocksDaily = 1000,
                ChainId = chainEth.Id,
                EndHeight = 20000,
                FineAmount = 0.ToString(),
                RewardRate = GameOfTrustTestData.RewardRate.ToString(),
                StartHeight = 10000,
                UnlockCycle = 10000,
                UnlockHeight = 0,
                DepositTokenId = tokenB.Id,
                HarvestTokenId = tokenB.Id,
                TotalAmountLimit = (BigInteger.Pow(10, tokenB.Decimals) * 20).ToString(),
                TotalValueLocked = 0.ToString(),
                UnlockMarketCap = unlockMarketCap
            });
        }

        public async Task initUser()
        {
        }
    }


    public class Token
    {
        public Token()
        {
        }

        public Token(string symbol, string address, int @decimal)
        {
            Symbol = symbol;
            Address = address;
            Decimal = @decimal;
        }

        public string Symbol { get; set; }
        public string Address { get; set; }
        public int Decimal { get; set; }
    }
}