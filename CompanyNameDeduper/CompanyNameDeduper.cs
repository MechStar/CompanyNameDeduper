using System.Text.RegularExpressions;

public partial class CompanyNameDeduper
{
    private readonly Dictionary<string, Dictionary<string, int>> _data = [];
    private static readonly Regex _clean = MyRegex();

    [GeneratedRegex("[^a-z0-9]")]
    private static partial Regex MyRegex();

    // order is important
    private readonly string[] _suffixes =
    [
        "ltd",
        "llc",
        "limitedliabilitycompany",
        "limited",
        "incorporated",
        "inc",
        "corporation",
        "corp",
        "company",
        "co"
    ];

    /// <summary>
    /// Reads input file of new-line-delimited company names, cleans them, and stores them in memory
    /// </summary>
    /// <param name="path">Path to file</param>
    /// <returns>True if successful, otherwise False</returns>
    public async Task<bool> ReadFromFileAsync(string path)
    {
        try
        {
            using var reader = new StreamReader(path);

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();

                if (!string.IsNullOrWhiteSpace(line))
                {
                    var clean = CleanCompanyName(line);

                    if (!string.IsNullOrWhiteSpace(clean))
                    {
                        StoreNextCompany(clean, line);
                    }
                }
            }
        }
        catch (Exception)
        {
            return false;
        }

        return true;
    }

    private string CleanCompanyName(string name)
    {
        name = name.ToLowerInvariant();
        name = _clean.Replace(name, string.Empty);

        foreach (var suffix in _suffixes)
            name = name.EndsWith(suffix) ? name[..^suffix.Length] : name;

        return name;
    }

    private void StoreNextCompany(string cleanName, string originalName)
    {
        if (_data.ContainsKey(cleanName))
        {
            if (_data[cleanName].ContainsKey(originalName))
            {
                _data[cleanName][originalName]++;
            }
            else
            {
                _data[cleanName].Add(originalName, 1);
            }
        }
        else
        {
            _data.Add(cleanName, new Dictionary<string, int> { { originalName, 1 } });
        }
    }

    /// <summary>
    /// Writes output file of new-line-delimited potential duplicate company names
    /// </summary>
    /// <param name="path">Path to file</param>
    /// <param name="type">Type of export</param>
    /// <param name="excludeLiteralDups">Set to exlude literal duplicates, i.e.: "Microsoft" and "Microsoft"</param>
    /// <param name="skipLineAfterDupGroup">Set to skip line after group of potential duplicates</param>
    /// <returns>True if successful, otherwise False</returns>
    public async Task<bool> WriteToFileAsync(string path, ExportType type = ExportType.Duplicates, bool excludeLiteralDups = false, bool skipLineAfterDupGroup = false)
    {
        return type switch
        {
            ExportType.Uniques => await WriteToFileAsync(path, x => x.Value.Count == 1 && x.Value.First().Value == 1, excludeLiteralDups, skipLineAfterDupGroup),
            ExportType.Duplicates => await WriteToFileAsync(path, x => x.Value.Count > 1 || x.Value.First().Value > 1, excludeLiteralDups, skipLineAfterDupGroup),
            _ => await WriteToFileAsync(path, x => true, excludeLiteralDups, skipLineAfterDupGroup),
        };
    }

    /// <summary>
    /// Writes output file of new-line-delimited potential duplicate company names
    /// </summary>
    /// <param name="path">Path to file</param>
    /// <param name="filter">Function for filter</param>
    /// <param name="excludeLiteralDups">Set to exlude literal duplicates, i.e.: "Microsoft" and "Microsoft"</param>
    /// <param name="skipLineAfterDupGroup">Set to skip line after group of potential duplicates</param>
    /// <returns>True if successful, otherwise False</returns>
    public async Task<bool> WriteToFileAsync(string path, Func<KeyValuePair<string, Dictionary<string, int>>, bool> filter, bool excludeLiteralDups = false, bool skipLineAfterDupGroup = false)
    {
        try
        {
            using var writer = new StreamWriter(path);

            // apply appropriate filter
            var data = _data.Where(filter);

            foreach (var item in data)
            {
                foreach (var variation in item.Value)
                {
                    for (var i = 0; i < variation.Value; i++)
                    {
                        await writer.WriteLineAsync(variation.Key);

                        // if we want to omit literal dups so not to repeat same exact string
                        if (excludeLiteralDups) break;
                    }
                }

                // if we want to visually group potential dups
                if (skipLineAfterDupGroup) await writer.WriteLineAsync();
            }
        }
        catch (Exception)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Clears internal data structure to prepare for new import/dedup/export
    /// </summary>
    public void ClearMemory() => _data.Clear();
}
