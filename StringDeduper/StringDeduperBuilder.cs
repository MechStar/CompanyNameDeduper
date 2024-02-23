using StringDeduper.Interfaces;

namespace StringDeduper;

public class StringDeduperBuilder : IStringDeduperBuilder, IStringDeduperService
{
    private Func<string, string>? GetKey;
    private string[]? IgnoredSuffixes;
    private readonly Dictionary<string, Dictionary<string, int>> _data = [];

    public IStringDeduperBuilder AddNormalizeStrategy(Func<string, string> getKey)
    {
        GetKey = getKey;
        return this;
    }

    public IStringDeduperBuilder AddIgnoredSuffixes(string[]? ignoredSuffixes = default)
    {
        IgnoredSuffixes = ignoredSuffixes;
        return this;
    }

    public IStringDeduperService Build()
    {
        return this;
    }

    public async Task ImportStrings(IAsyncEnumerable<string> input)
    {
        await foreach (var str in input)
        {
            var key = GetKey == null ? str : GetKey(str);

            if (IgnoredSuffixes != null)
                foreach (var suffix in IgnoredSuffixes)
                    key = key.EndsWith(suffix) ? key[..^suffix.Length] : key;

            SaveString(key, str);
        }
    }

    public IEnumerable<string> GetDuplicates(bool includeOriginal = false, bool excludeRepeats = false, bool skipLine = false)
    {
        var data = _data.Where(x => x.Value.First().Value > 1 || x.Value.Count > 1);

        foreach (var item in data)
        {
            var variations = item.Value;

            for (var i = 0; i < variations.Count; i++)
            {
                var variationFrequency = variations.ElementAt(i).Value;

                for (var j = 0; j < variationFrequency; j++)
                {
                    if (!includeOriginal && i == 0 && j == 0) continue;

                    yield return variations.ElementAt(i).Key;

                    if (excludeRepeats) break;
                }
            }

            if (skipLine) yield return string.Empty;
        }
    }

    public IEnumerable<string> GetUniques(bool restrictToUniqueInput = false)
    {
        var data = _data.Where(x => !restrictToUniqueInput || (x.Value.First().Value == 1 && x.Value.Count == 1));

        foreach (var item in _data)
            yield return item.Value.First().Key;
    }

    private void SaveString(string key, string value)
    {
        if (_data.ContainsKey(key))
        {
            if (_data[key].ContainsKey(value))
            {
                _data[key][value]++;
            }
            else
            {
                _data[key].Add(value, 1);
            }
        }
        else
        {
            _data.Add(key, new Dictionary<string, int> { { value, 1 } });
        }
    }
}
