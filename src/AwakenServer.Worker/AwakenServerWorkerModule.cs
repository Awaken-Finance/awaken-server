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
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<TradePairSyncWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<TradePairEventSyncWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<TradeRecordEventSwapWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<TradeRecordRevertWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<MarketSnapshotWorker>());
        }
    }
}