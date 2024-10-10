using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Serilog;

namespace AwakenServer.CoinGeckoApi;

public class HandlerExceptionService
{
    public static async Task<FlowBehavior> HandleWithReturn(Exception ex)
    {
        Log.Error(ex, $"Handled exception: {ex.Message}");
        Console.WriteLine($"Handled exception: {ex.Message}");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return
        };
    }
    
    public static async Task<FlowBehavior> HandleWithThrow(Exception ex)
    {
        Log.Error(ex, $"Handled exception: {ex.Message}");
        Console.WriteLine($"Handled exception: {ex.Message}");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw
        };
    }
    
    public static async Task<FlowBehavior> HandleWithReThrow(Exception ex)
    {
        Log.Error(ex, $"Handled exception: {ex.Message}");
        Console.WriteLine($"Handled exception: {ex.Message}");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Rethrow
        };
    }
    
    public static async Task<FlowBehavior> HandleByReturn0(Exception ex)
    {
        Log.Error(ex, $"Handled exception: {ex.Message}");
        Console.WriteLine($"Handled exception: {ex.Message}");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = 0
        };
    }
    
    public static async Task<FlowBehavior> HandleByReturnMinusOne(Exception ex)
    {
        Log.Error(ex, $"Handled exception: {ex.Message}");
        Console.WriteLine($"Handled exception: {ex.Message}");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = -1
        };
    }
}