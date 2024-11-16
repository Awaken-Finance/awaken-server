using System.Collections.Generic;
using Awaken.Common.HttpClient;
using AwakenServer.Commons;
using AwakenServer.Signature.Provider;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Volo.Abp.Modularity;
using System.Net.Http;
using AwakenServer.Signature.Options;
using Newtonsoft.Json;

namespace AwakenServer;

[DependsOn(
    typeof(AwakenServerApplicationContractsModule)
)]
public class AwakenServerSignatureTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddLogging(configure => configure.AddConsole());
        
        var mockHttpProvider = new Mock<IHttpProvider>();
        mockHttpProvider.Setup(provider => provider.InvokeAsync<CommonResponseDto<SignResponseDto>>(
            It.IsAny<HttpMethod>(),
            It.IsAny<string>(), 
            It.IsAny<Dictionary<string, string>>(), 
            It.IsAny<Dictionary<string, string>>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, string>>(), 
            It.IsAny<JsonSerializerSettings>(), 
            It.IsAny<int?>(),
            It.IsAny<bool>(), 
            It.IsAny<bool>() 
        )).ReturnsAsync(new CommonResponseDto<SignResponseDto> { Code = "20000", Data = new SignResponseDto()
        {
            Signature = "123"
        }});
        
        Configure<SignatureServerOptions>(options =>
        {
            options.BaseUrl = "test";
            options.AppId = "test";
            options.AppSecret = "test";
        });
        
        context.Services.AddSingleton<IHttpProvider>(mockHttpProvider.Object);
        context.Services.AddSingleton<ISignatureProvider, SignatureProvider>();
    }
}