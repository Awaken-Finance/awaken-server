using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using AElf.ExceptionHandler;
using Newtonsoft.Json;
using Awaken.Samples.HttpClient;
using AwakenServer;
using AwakenServer.Commons;
using Serilog;
using Volo.Abp.DependencyInjection;

namespace Awaken.Common.HttpClient;

public interface IHttpProvider : ISingletonDependency
{
    Task<T> InvokeAsync<T>(string domain, ApiInfo apiInfo,
        Dictionary<string, string> pathParams = null,
        Dictionary<string, string> param = null,
        string body = null,
        Dictionary<string, string> header = null, JsonSerializerSettings settings = null, int? timeout = null,
        bool withInfoLog = false, bool withDebugLog = true);

    Task<T> InvokeAsync<T>(HttpMethod method, string url,
        Dictionary<string, string> pathParams = null,
        Dictionary<string, string> param = null,
        string body = null,
        Dictionary<string, string> header = null, JsonSerializerSettings settings = null, int? timeout = null,
        bool withInfoLog = false, bool withDebugLog = true);

    Task<string> InvokeAsync(string domain, ApiInfo apiInfo,
        Dictionary<string, string> pathParams = null,
        Dictionary<string, string> param = null,
        string body = null,
        Dictionary<string, string> header = null, JsonSerializerSettings settings = null, int? timeout = null,
        bool withInfoLog = false, bool withDebugLog = true);

    Task<string> InvokeAsync(HttpMethod method, string url,
        Dictionary<string, string> pathParams = null,
        Dictionary<string, string> param = null,
        string body = null,
        Dictionary<string, string> header = null, int? timeout = null, bool withInfoLog = false,
        bool withDebugLog = true);

    Task<HttpResponseMessage> InvokeResponseAsync(HttpMethod method, string url,
        Dictionary<string, string> pathParams = null,
        Dictionary<string, string> param = null,
        string body = null,
        Dictionary<string, string> header = null,
        int? timeout = null,
        bool withLog = false, bool debugLog = true);

    Task<HttpResponseMessage> InvokeResponseAsync(string domain, ApiInfo apiInfo,
        Dictionary<string, string> pathParams = null,
        Dictionary<string, string> param = null,
        string body = null,
        Dictionary<string, string> header = null, JsonSerializerSettings settings = null, int? timeout = null,
        bool withLog = false,
        bool debugLog = true);
}

public class HttpProvider : IHttpProvider
{
    public static readonly JsonSerializerSettings DefaultJsonSettings = JsonSettingsBuilder.New()
        .WithCamelCasePropertyNamesResolver()
        .IgnoreNullValue()
        .Build();

    private const int DefaultTimeout = 5000;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;

    public HttpProvider(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
        _logger = Log.ForContext<HttpProvider>();
    }

    
    [ExceptionHandler(typeof(Exception),
        TargetType = typeof(HandlerExceptionService), MethodName = nameof(HandlerExceptionService.HandleWithHttpException))]
    public virtual async Task<T> InvokeAsync<T>(string domain, ApiInfo apiInfo,
        Dictionary<string, string> pathParams = null,
        Dictionary<string, string> param = null,
        string body = null,
        Dictionary<string, string> header = null, JsonSerializerSettings settings = null, int? timeout = null,
        bool withInfoLog = false, bool withDebugLog = true)
    {
        var resp = await InvokeAsync(apiInfo.Method, domain + apiInfo.Path, pathParams, param, body, header, timeout,
            withInfoLog, withDebugLog);
        return JsonConvert.DeserializeObject<T>(resp, settings ?? DefaultJsonSettings);
    }
    
    [ExceptionHandler(typeof(Exception),
        TargetType = typeof(HandlerExceptionService), MethodName = nameof(HandlerExceptionService.HandleWithHttpException))]
    public virtual async Task<T> InvokeAsync<T>(HttpMethod method, string url,
        Dictionary<string, string> pathParams = null,
        Dictionary<string, string> param = null,
        string body = null,
        Dictionary<string, string> header = null, JsonSerializerSettings settings = null, int? timeout = null,
        bool withInfoLog = false, bool withDebugLog = true)
    {
        var resp = await InvokeAsync(method, url, pathParams, param, body, header, timeout, withInfoLog, withDebugLog);
        return JsonConvert.DeserializeObject<T>(resp, settings ?? DefaultJsonSettings);
    }

    public async Task<HttpResponseMessage> InvokeResponseAsync(string domain, ApiInfo apiInfo,
        Dictionary<string, string> pathParams = null,
        Dictionary<string, string> param = null,
        string body = null,
        Dictionary<string, string> header = null, JsonSerializerSettings settings = null, int? timeout = null,
        bool withLog = false, bool debugLog = true)
    {
        return await InvokeResponseAsync(apiInfo.Method, domain + apiInfo.Path, pathParams, param, body, header,
            timeout, withLog, debugLog);
    }

    public async Task<string> InvokeAsync(string domain, ApiInfo apiInfo,
        Dictionary<string, string> pathParams = null,
        Dictionary<string, string> param = null,
        string body = null,
        Dictionary<string, string> header = null, JsonSerializerSettings settings = null, int? timeout = null,
        bool withInfoLog = false, bool withDebugLog = true)
    {
        return await InvokeAsync(apiInfo.Method, domain + apiInfo.Path, pathParams, param, body, header, timeout,
            withInfoLog, withDebugLog);
    }

    public async Task<string> InvokeAsync(HttpMethod method, string url,
        Dictionary<string, string> pathParams = null,
        Dictionary<string, string> param = null,
        string body = null,
        Dictionary<string, string> header = null,
        int? timeout = null,
        bool withInfoLog = false, bool withDebugLog = true)
    {
        var response = await InvokeResponseAsync(method, url, pathParams, param, body, header, timeout, withInfoLog,
            withDebugLog);
        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Server [{url}] returned status code {response.StatusCode} : {content}", null, response.StatusCode);
        }

        return content;
    }

    public async Task<HttpResponseMessage> InvokeResponseAsync(HttpMethod method, string url,
        Dictionary<string, string> pathParams = null,
        Dictionary<string, string> param = null,
        string body = null,
        Dictionary<string, string> header = null,
        int? timeout = null,
        bool withLog = false, bool debugLog = true)
    {
        // url params
        var fullUrl = PathParamUrl(url, pathParams);

        var builder = new UriBuilder(fullUrl);
        var query = HttpUtility.ParseQueryString(builder.Query);
        foreach (var item in param ?? new Dictionary<string, string>())
            query[item.Key] = item.Value;
        builder.Query = query.ToString() ?? string.Empty;

        var request = new HttpRequestMessage(method, builder.ToString());

        // headers
        foreach (var h in header ?? new Dictionary<string, string>())
            request.Headers.Add(h.Key, h.Value);

        // body
        if (body != null)
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        // send
        var stopwatch = Stopwatch.StartNew();
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromMilliseconds(timeout ?? DefaultTimeout);
        var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        var time = stopwatch.ElapsedMilliseconds;
        // log
        if (withLog)
            _logger.Information(
                "Request To {FullUrl}, statusCode={StatusCode}, time={Time}, query={Query}, body={Body}, resp={Content}",
                fullUrl, response.StatusCode, time, builder.Query, body, content);
        else if (debugLog)
            _logger.Debug(
                "Request To {FullUrl}, statusCode={StatusCode}, time={Time}, query={Query}, body={Body}, resp={Content}",
                fullUrl, response.StatusCode, time, builder.Query,  body, content);
        return response;
    }
    
    private static string PathParamUrl(string url, Dictionary<string, string> pathParams)
    {
        return pathParams.IsNullOrEmpty()
            ? url
            : pathParams.Aggregate(url, (current, param) => current.Replace($"{{{param.Key}}}", param.Value));
    }
}