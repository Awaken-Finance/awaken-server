using Awaken.Common.HttpClient;
using AwakenServer.Common;
using AwakenServer.Commons;
using AwakenServer.Signature.Options;
using CAServer.Common;
using CAServer.Commons;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Serilog;

namespace AwakenServer.Signature.Provider;

public interface ISignatureProvider
{
    Task<string> SignTxMsg(string publicKey, string hexMsg);
}

public class SignatureProvider : ISignatureProvider, ISingletonDependency
{
    private const string GetSecurityUri = "/api/app/signature";
    private const string EmptyString = "";

    private readonly IOptionsMonitor<SignatureServerOptions> _signatureServerOptions;
    private readonly IHttpProvider _httpProvider;
    private readonly ILogger _logger;
    
    public SignatureProvider(IOptionsMonitor<SignatureServerOptions> signatureOptions,
        IHttpProvider httpProvider)
    {
        _signatureServerOptions = signatureOptions;
        _logger = Log.ForContext<SignatureProvider>();
        _httpProvider = httpProvider;
    }

    private string Uri(string path)
    {
        return _signatureServerOptions.CurrentValue.BaseUrl.TrimEnd('/') + path;
    }

    public async Task<string> SignTxMsg(string publicKey, string hexMsg)
    {
        var signatureSend = new SendSignatureDto
        {
            PublicKey = publicKey,
            HexMsg = hexMsg,
        };

        var resp = await _httpProvider.InvokeAsync<CommonResponseDto<SignResponseDto>>(HttpMethod.Post,
            Uri(GetSecurityUri), 
            body: JsonConvert.SerializeObject(signatureSend),
            header: SecurityServerHeader()
            );
        AssertHelper.IsTrue(resp?.Success ?? false, "Signature response failed");
        AssertHelper.NotEmpty(resp!.Data?.Signature, "Signature response empty");
        return resp.Data!.Signature;
    }
    
    public Dictionary<string, string> SecurityServerHeader(params string[] signValues)
    {
        var signString = string.Join(EmptyString, signValues);
        return new Dictionary<string, string>
        {
            ["appid"] = _signatureServerOptions.CurrentValue.AppId,
            ["signature"] = EncryptionHelper.EncryptHex(signString, _signatureServerOptions.CurrentValue.AppSecret)
        };
    }
}

public class SendSignatureDto
{
    public string PublicKey { get; set; }
    public string HexMsg { get; set; }
}

public class SignResponseDto
{
    public string Signature { get; set; }
}