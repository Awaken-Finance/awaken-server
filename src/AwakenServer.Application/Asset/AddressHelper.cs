using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace AwakenServer.Asset;

public class AddressHelper : ISingletonDependency
{
    [ExceptionHandler(typeof(Exception),
        TargetType = typeof(AddressHelper), MethodName = nameof(HandleException))]
    public virtual async Task CheckAddressAsync(string address)
    {
        AElf.Types.Address.FromBase58(address);
    }
    
    public static async Task<FlowBehavior> HandleException(Exception ex)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
            ReturnValue = new UserFriendlyException("Address is invalid")
        };
    }
}