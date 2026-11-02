using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Reqnroll;

namespace Tfl.DummyJson.ApiTests.Support;

[Binding]
public sealed class ApiWorld : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _http = new();

    public Uri BaseUri { get; private set; } = new(Environment.GetEnvironmentVariable("DUMMYJSON_BASE_URL") ?? "https://dummyjson.com/");
    public HttpResponseMessage? Response { get; set; }
    public JsonDocument? Body { get; set; }
    public string? AccessToken { get; set; }
    public object? LastRequest { get; set; }

    public async Task SendAsync(HttpMethod method, string path, object? payload = null, bool authenticated = false)
    {
        Response?.Dispose();
        Body?.Dispose();
        using var request = new HttpRequestMessage(method, new Uri(BaseUri, path));
        if (payload is not null)
            request.Content = JsonContent.Create(payload, options: JsonOptions);
        if (authenticated)
        {
            if (string.IsNullOrWhiteSpace(AccessToken))
                throw new InvalidOperationException("No access token is available. Authenticate before making this request.");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
        }

        LastRequest = payload;
        Response = await _http.SendAsync(request);
        var content = await Response.Content.ReadAsStringAsync();
        Body = JsonDocument.Parse(string.IsNullOrWhiteSpace(content) ? "{}" : content);
    }

    public JsonElement Root => Body?.RootElement ?? throw new InvalidOperationException("No JSON response has been received.");

    public void Dispose()
    {
        Response?.Dispose();
        Body?.Dispose();
        _http.Dispose();
    }
}
