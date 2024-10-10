using System;
using System.Net.Http;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Serilog;

namespace AwakenServer;

public class HandlerExceptionService
{
    public static async Task<FlowBehavior> HandleWithReturn(Exception ex)
    {
        Log.Error(ex, $"Handled exception: {ex.Message}");
        Console.WriteLine($"Handled exception: {ex.Message}");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
        };
    }
    
    public static async Task<FlowBehavior> HandleWithReturnNull(Exception ex)
    {
        Log.Error(ex, $"Handled exception: {ex.Message}");
        Console.WriteLine($"Handled exception: {ex.Message}");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = null
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
    
    public static async Task<FlowBehavior> HandleWithHttpException(Exception ex)
    {
        Log.Error(ex, $"Handled exception: {ex.Message}");
        Console.WriteLine($"Handled exception: {ex.Message}");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
            ReturnValue = new HttpRequestException(ex.Message)
        };
    }
    
    public static async Task<FlowBehavior> HandleWithReturn0(Exception ex)
    {
        Log.Error(ex, $"Handled exception: {ex.Message}");
        Console.WriteLine($"Handled exception: {ex.Message}");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = 0
        };
    }
    
    public static async Task<FlowBehavior> HandleWithReturnMinusOne(Exception ex)
    {
        Log.Error(ex, $"Handled exception: {ex.Message}");
        Console.WriteLine($"Handled exception: {ex.Message}");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = -1
        };
    }
    
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