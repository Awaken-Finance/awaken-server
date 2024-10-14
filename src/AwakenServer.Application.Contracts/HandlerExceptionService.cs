using System;
using System.Net.Http;
using System.Threading.Tasks;
using AElf.ExceptionHandler;

namespace AwakenServer;

public class HandlerExceptionService
{
    public static async Task<FlowBehavior> HandleWithReturn(Exception ex)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
        };
    }
    
    public static async Task<FlowBehavior> HandleWithReturnNull(Exception ex)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = null
        };
    }
    
    public static async Task<FlowBehavior> HandleWithReThrow(Exception ex)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Rethrow
        };
    }
    
    public static async Task<FlowBehavior> HandleWithHttpException(Exception ex)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
            ReturnValue = new HttpRequestException(ex.Message)
        };
    }
    
    public static async Task<FlowBehavior> HandleWithReturn0(Exception ex)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = 0
        };
    }
    
    public static async Task<FlowBehavior> HandleWithReturnMinusOne(Exception ex)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = -1
        };
    }
    
    public static async Task<FlowBehavior> HandleWithReturnBool(Exception ex)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = false
        };
    }
}