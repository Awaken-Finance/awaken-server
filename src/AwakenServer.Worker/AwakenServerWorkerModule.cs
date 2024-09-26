using AwakenServer.Worker.DataCleanup;
using AwakenServer.Worker.IndexerReSync;
using AwakenServer.Worker.IndexerSync;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Modularity;

namespace AwakenServer.Worker
{
    [DependsOn(
        typeof(AwakenServerApplicationContractsModule),
        typeof(AbpBackgroundWorkersModule)
    )]
    public class AwakenServerWorkerModule : AbpModule
    {
        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var backgroundWorkerManger = context.ServiceProvider.GetRequiredService<IBackgroundWorkerManager>();
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<TradePairUpdateWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<LiquidityEventSyncWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<TradePairEventSyncWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<SyncEventSyncWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<TradeRecordEventSwapWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<TransactionRevertWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<PortfolioEventSyncWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<InternalTokenPriceBuildWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<UserLiquidityUpdateWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<DataCleanupWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<PortfolioEventReSyncWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<SwapEventReSyncWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<ActivityEventSyncWorker>());
        }
    }
}