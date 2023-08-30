using System.Threading.Tasks;
using AwakenServer.Debits.Entities.Ef;
using AwakenServer.Debits.Options;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.ContractEventHandler.Services
{
    public class DebitInitializeService : ITransientDependency
    {
        private readonly DebitOption _debitOption;
        private readonly IRepository<CompController> _compControllerRepository;

        public DebitInitializeService(IRepository<CompController> compControllerRepository,
            IOptionsSnapshot<DebitOption> debitOption)
        {
            _compControllerRepository = compControllerRepository;
            _debitOption = debitOption.Value;
        }

        public async Task InitializeCompControllerAsync()
        {
            if (!_debitOption.IsResetData)
            {
                return;
            }

            foreach (var compController in _debitOption.CompControllerList)
            {
                var targetCompController = await _compControllerRepository.FindAsync(x =>
                    x.ControllerAddress == compController.ControllerAddress && x.ChainId == compController.ChainId);
                if (targetCompController != null)
                {
                    return;
                }

                await _compControllerRepository.InsertAsync(new CompController
                {
                    ChainId = compController.ChainId,
                    ControllerAddress = compController.ControllerAddress,
                    CloseFactorMantissa = compController.CloseFactorMantissa,
                    DividendTokenId = compController.CompTokenId
                });
            }
        }
    }
}