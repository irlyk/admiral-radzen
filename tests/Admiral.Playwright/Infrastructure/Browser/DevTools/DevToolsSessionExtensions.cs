namespace Admiral.Playwright.Infrastructure.Browser.DevTools;

internal static class DevToolsSessionExtensions
{
    private static readonly JsonSerializerOptions _responseOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task SendCommandAsync(this ICDPSession session, string command, object? parameters = null)
    {
        await session.SendAsync(command, Parameters(parameters));
    }

    public static async Task<T> SendCommandAsync<T>(this ICDPSession session, string command, object? parameters = null)
        where T : class
    {
        var response = await session.SendAsync(command, Parameters(parameters));
        return response?.Deserialize<T>(_responseOptions) ?? throw new AssertionException($"CDP command {command} returned no {typeof(T).Name}");
    }

    private static Dictionary<string, object>? Parameters(object? value)
    {
        return value is null ? null : JsonSerializer.SerializeToElement(value).EnumerateObject()
            .ToDictionary(property => property.Name, property => (object)property.Value);
    }
}
