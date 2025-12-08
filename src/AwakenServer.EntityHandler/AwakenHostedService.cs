using System;
using System.Threading;
using System.Threading.Tasks;
using AwakenServer.EntityHandler.Services;
using AwakenServer.Trade;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Volo.Abp;

namespace AwakenServer.EntityHandler
{
    public class AwakenHostedService : IHostedService
    {
        private readonly IAbpApplicationWithExternalServiceProvider _application;
        private readonly IServiceProvider _serviceProvider;

        public AwakenHostedService(
            IAbpApplicationWithExternalServiceProvider application,
            IServiceProvider serviceProvider)
        {
            _application = application;
            _serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _application.InitializeAsync(_serviceProvider);
            var chainDataInitializer = _serviceProvider.GetRequiredService<ChainInitializeService>();
            await chainDataInitializer.InitializeDataAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _application.Shutdown();
            return Task.CompletedTask;
        }
    }
}