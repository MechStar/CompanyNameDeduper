namespace StringDeduper.Helpers;

public static class HttpUtility
{
    public static async IAsyncEnumerable<string> ReadAsync(Stream stream)
    {
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
            yield return await reader.ReadLineAsync() ?? string.Empty;
    }
}
