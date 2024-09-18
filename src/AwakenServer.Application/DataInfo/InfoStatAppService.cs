using System;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;

namespace AwakenServer.DataInfo;

public class InfoStatAppService
{
    public async void CreateLiquidityRecordAsync(LiquidityRecordDto liquidityRecordDto)
    {
        // insert transaction
    }
    
    public async void CreateSwapRecordAsync(SwapRecordDto swapRecordDto)
    {
        // insert transaction
        // pool lpFee/volume
        // follow token volume
        // global volume
    }
    
    public async void CreateSyncRecordAsync(SyncRecordDto syncRecordDto)
    {
        // pool tvl/price
        // follow token price/tvl
        // global tvl
    }

    public async void RefreshTvlAsync()
    {
        // refresh token tvl
        // refresh global tvl
    }

    public async void InitTokenFollowPairAsync()
    {
        
    }
}