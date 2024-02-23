namespace StringDeduper.Helpers;

public static class FileUtility
{
    public static async IAsyncEnumerable<string> ReadAsync(string path)
    {
        using var reader = new StreamReader(path);

        while (!reader.EndOfStream)
            yield return await reader.ReadLineAsync() ?? string.Empty;
    }

    public static async Task WriteAsync(string path, IEnumerable<string> output)
    {
        using var writer = new StreamWriter(path);

        foreach (var item in output)
            await writer.WriteLineAsync(item);
    }
}
