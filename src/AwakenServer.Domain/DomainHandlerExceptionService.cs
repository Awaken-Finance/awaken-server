using System;
using System.Net.Http;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Serilog;

namespace AwakenServer;

public class DomainHandlerExceptionService
{
    public static async Task<FlowBehavior> HandleWithReturnBool(Exception ex)
    {
        Log.Error(ex, $"Handled exception: {ex.Message}");
        Console.WriteLine($"Handled exception: {ex.Message}");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = false
        };
    }
}