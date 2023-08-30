using System;
using System.Numerics;
using Nethereum.Util;

namespace AwakenServer.Farms.Helpers
{
    public static class ProjectTokenCalculationHelper
    {
        public static int LastTerm { get; set; }
        public static (BigInteger usdtPending, BigInteger projectTokenPending) EstimatePendingRevenue(FarmType farmType,
            long startBlock, long halvingPeriod1, long halvingPeriod2, BigInteger tokenPerBlock1, BigInteger tokenPerBlock2,
            int totalWeight, long usdtStartBlock, long usdtEndBlock, BigInteger usdtDividendPerBlock,
            long lastRewardBlock, int poolWeight,
            long currentBlockHeight)
        {
            var usdtPending = BigInteger.Zero;
            var tokenPending = BigInteger.Zero;
            if (totalWeight == 0 || poolWeight == 0)
            {
                return (usdtPending, tokenPending);
            }

            usdtPending = GetUsdtDividend(usdtStartBlock, usdtEndBlock,
                usdtDividendPerBlock,
                lastRewardBlock,
                currentBlockHeight) * poolWeight / totalWeight;

            tokenPending += farmType switch
            {
                FarmType.Massive => GetMassiveProjectTokenReward(startBlock, lastRewardBlock, currentBlockHeight,
                    halvingPeriod1, halvingPeriod2, tokenPerBlock1,
                    tokenPerBlock2),
                FarmType.Compound => GetCompoundProjectTokenReward(startBlock, lastRewardBlock,
                    currentBlockHeight, halvingPeriod1, tokenPerBlock1),
                _ => throw new ArgumentOutOfRangeException()
            };
            tokenPending = tokenPending * poolWeight / totalWeight;
            return (usdtPending, tokenPending);
        }

        public static decimal CalculatePoolProjectTokenApyWithoutPrice(int pid, int poolTokenDecimal, string deposit, FarmType farmType,
            long startBlock, long halvingPeriod1, long halvingPeriod2, BigInteger tokenPerBlock1, BigInteger tokenPerBlock2,
            int totalWeight,
            int poolWeight,
            long currentBlockHeight)
        {
            var tokenDecimal = poolTokenDecimal;
            var blocksToEnd = GetBlocksToTermEnd(startBlock, halvingPeriod1, halvingPeriod2, currentBlockHeight);
            var to = blocksToEnd + currentBlockHeight;
            var (_, tokenToDividend) = EstimatePendingRevenue(farmType,
                startBlock, halvingPeriod1, halvingPeriod2, tokenPerBlock1, tokenPerBlock2,
                totalWeight, 0, 0, BigInteger.Zero,
                currentBlockHeight, poolWeight,
                to);
            if (farmType == FarmType.Massive)
            {
                return GetPoolApyWithoutPrice(tokenToDividend, deposit, tokenDecimal,
                    FarmConstant.ProjectTokenDecimal, 365 * FarmConstants.BlocksPerDay, blocksToEnd);
            }

            var term = halvingPeriod1 + halvingPeriod2;
            if (pid == 0 && startBlock + term - 1 >= currentBlockHeight)
            {
                return GetPoolApyWithoutPrice(tokenToDividend, deposit, tokenDecimal,
                    FarmConstant.ProjectTokenDecimal, 365 * FarmConstants.BlocksPerDay, blocksToEnd);
            }

            return GetPoolApyWithoutPrice(tokenToDividend, deposit, tokenDecimal,
                FarmConstant.ProjectTokenDecimal);
        }

        public static decimal CalculatePoolUsdtApyWithoutPrice(int poolTokenDecimal, string usdtDividendPerBlock,
            long usdtDividendStartBlockHeight, long usdtDividendEndBlockHeight, int poolWeight, int totalWeight,
            string deposit, long currentBlock)
        {
            var tokenDecimal = poolTokenDecimal;
            if (string.IsNullOrEmpty(usdtDividendPerBlock) ||
                usdtDividendEndBlockHeight <= currentBlock)
            {
                return 0m;
            }

            var usdtPerBlock = BigInteger.Parse(usdtDividendPerBlock);
            var startBlock = usdtDividendStartBlockHeight >= currentBlock
                ? usdtDividendStartBlockHeight
                : currentBlock;
            var endBlock = usdtDividendEndBlockHeight;
            var totalUsdt = usdtPerBlock * (endBlock - startBlock) * poolWeight / totalWeight;
            return GetPoolApyWithoutPrice(totalUsdt, deposit, tokenDecimal,
                FarmConstant.UsdtDecimal);
        }

        public static decimal GetPoolApyWithoutPrice
        (BigInteger tokenToDividend, string depositAmount, int tokenDecimal, int pendingTokenDecimal,
            int numerator = 1, int denominator = 1)
        {
            if (depositAmount == FarmConstants.ZeroBalance || tokenToDividend == BigInteger.Zero)
            {
                return 0;
            }

            decimal apy;
            if (tokenDecimal >= pendingTokenDecimal)
            {
                apy = (decimal)(tokenToDividend / BigDecimal.Parse(depositAmount) *
                    BigInteger.Pow(10, tokenDecimal - pendingTokenDecimal)
                    * numerator / denominator);
            }
            else
            {
                apy = (decimal)(tokenToDividend / BigDecimal.Parse(depositAmount) /
                    BigInteger.Pow(10, pendingTokenDecimal - tokenDecimal)
                    * numerator / denominator);
            }

            return decimal.Round(apy, 8);
        }

        public static BigInteger GetUsdtDividend(long usdtStartBlock, long usdtEndBlock,
            BigInteger usdPerBlock,
            long lastRewardBlock,
            long currentBlockHeight)
        {
            if (usdPerBlock == BigInteger.Zero)
            {
                return BigInteger.Zero;
            }

            if (currentBlockHeight < usdtStartBlock)
            {
                return BigInteger.Zero;
            }

            var realEndBlock = currentBlockHeight > usdtEndBlock ? usdtEndBlock : currentBlockHeight;
            var realStartBlock = lastRewardBlock > usdtStartBlock ? lastRewardBlock + 1 : usdtStartBlock;
            if (realEndBlock < realStartBlock)
            {
                return BigInteger.Zero;
            }

            return usdPerBlock * (realEndBlock - realStartBlock + 1);
        }

        public static BigInteger GetMassiveProjectTokenReward(long startBlock, long fromBlockHeight, long toBlockHeight,
            long halvingPeriod0,
            long halvingPeriod1,
            BigInteger tokenPerBlockConcentratedMining, BigInteger tokenPerBlockContinuousMining)
        {
            if (startBlock > toBlockHeight)
            {
                return BigInteger.Zero;
            }

            var blockReward = BigInteger.Zero;
            var blockLockReward = BigInteger.Zero;
            var halvingPeriod = halvingPeriod0 + halvingPeriod1;
            var n = MassivePhase(startBlock, fromBlockHeight, halvingPeriod0, halvingPeriod1);
            var m = MassivePhase(startBlock, toBlockHeight, halvingPeriod0, halvingPeriod1);
            var switchBlock = 0L;
            while (n < m)
            {
                n++;
                var r = n * halvingPeriod + startBlock;
                switchBlock = (n - 1) * halvingPeriod + startBlock + halvingPeriod0;
                if (switchBlock > fromBlockHeight)
                {
                    blockLockReward += (switchBlock - fromBlockHeight) *
                                       GetRewardPerBlockForMassiveProjectTokenReward(tokenPerBlockConcentratedMining, n - 1);
                    blockReward += (r - switchBlock) * GetRewardPerBlockForMassiveProjectTokenReward(tokenPerBlockContinuousMining, n - 1);
                }
                else
                {
                    blockReward += (r - fromBlockHeight) * GetRewardPerBlockForMassiveProjectTokenReward(tokenPerBlockContinuousMining, n - 1);
                }

                fromBlockHeight = r;
            }

            switchBlock = m * halvingPeriod + startBlock + halvingPeriod0;

            if (switchBlock >= toBlockHeight)
            {
                blockLockReward += (toBlockHeight - fromBlockHeight) *
                                   GetRewardPerBlockForMassiveProjectTokenReward(tokenPerBlockConcentratedMining, m);
            }
            else
            {
                if (switchBlock > fromBlockHeight)
                {
                    blockLockReward += (switchBlock - fromBlockHeight) *
                                       GetRewardPerBlockForMassiveProjectTokenReward(tokenPerBlockConcentratedMining, m);
                    blockReward += (toBlockHeight - switchBlock) *
                                   GetRewardPerBlockForMassiveProjectTokenReward(tokenPerBlockContinuousMining, m);
                }
                else
                {
                    blockReward += (toBlockHeight - fromBlockHeight) *
                                   GetRewardPerBlockForMassiveProjectTokenReward(tokenPerBlockContinuousMining, m);
                }
            }

            return blockReward + blockLockReward;
        }

        public static BigInteger GetCompoundProjectTokenReward(long startBlock, long fromBlockHeight, long toBlockHeight,
            long halvingPeriod,
            BigInteger tokenPerBlock)
        {
            var blockReward = BigInteger.Zero;
            var n = CompoundPhase(startBlock, fromBlockHeight, halvingPeriod);
            var m = CompoundPhase(startBlock, toBlockHeight, halvingPeriod);
            while (n < m)
            {
                n++;
                var r = n * halvingPeriod + startBlock;
                blockReward += (r - fromBlockHeight) * Reward(startBlock, r, halvingPeriod, tokenPerBlock);
                fromBlockHeight = r;
            }
            
            blockReward += Reward(startBlock, toBlockHeight,
                halvingPeriod, tokenPerBlock) * (toBlockHeight - fromBlockHeight);
            return blockReward;
        }

        public static int CompoundPhase(long startBlock, long blockNumber, long halvingPeriod)
        {
            if (halvingPeriod == 0)
            {
                return 0;
            }

            if (blockNumber > startBlock)
            {
                return (int)((blockNumber - startBlock - 1) / halvingPeriod);
            }

            return 0;
        }

        public static int MassivePhase(long startBlock, long blockNumber, long halvingPeriod1, long halvingPeriod2)
        {
            var halvingPeriod = halvingPeriod1 + halvingPeriod2;
            if (halvingPeriod == 0)
            {
                return 0;
            }

            if (blockNumber > startBlock)
            {
                return (int)((blockNumber - startBlock - 1) / halvingPeriod);
            }

            return 0;
        }

        public static BigInteger Reward(long startBlock, long blockNumber, long halvingPeriod, BigInteger tokenPerBlock)
        {
            var phase = CompoundPhase(startBlock, blockNumber, halvingPeriod);
            if (phase > LastTerm)
            {
                return BigInteger.Zero;
            }
            return tokenPerBlock / BigInteger.Pow(2, phase);
        }

        public static BigInteger GetRewardPerBlockForMassiveProjectTokenReward(BigInteger tokenPerBlock, int phase)
        {
            if (phase > LastTerm)
            {
                return BigInteger.Zero;
            }
            return tokenPerBlock / BigInteger.Pow(2, phase);
        }

        public static int GetBlocksToTermEnd(long startBlock, long miningHalvingPeriod1, long miningHalvingPeriod2,
            long currentHeight)
        {
            var totalBlock = currentHeight - startBlock + 1;
            long currentPhaseBlock;
            int blocksToEnd;
            if (miningHalvingPeriod2 == 0)
            {
                currentPhaseBlock = totalBlock % miningHalvingPeriod1;
                blocksToEnd = (int)(miningHalvingPeriod1 - currentPhaseBlock);
                return blocksToEnd;
            }

            var term = miningHalvingPeriod1 + miningHalvingPeriod2;
            currentPhaseBlock = totalBlock % term;
            var isFirstPhase = currentPhaseBlock <= miningHalvingPeriod1;
            blocksToEnd = isFirstPhase
                ? (int)(miningHalvingPeriod1 - currentPhaseBlock)
                : (int)(term - currentPhaseBlock);
            return blocksToEnd;
        }
    }
}