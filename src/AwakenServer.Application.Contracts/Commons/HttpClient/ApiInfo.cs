using System.Net.Http;

namespace Awaken.Samples.HttpClient;

public class ApiInfo
{
    public string Path { get; set; }
    public HttpMethod Method { get; set; }

    public ApiInfo(HttpMethod method, string path, string name = null)
    {
        Path = path;
        Method = method;
    }
}