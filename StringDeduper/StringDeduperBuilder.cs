using Quickenshtein;
using StringDeduper.Interfaces;

namespace StringDeduper;

public class StringDeduperBuilder : IStringDeduperBuilder, IStringDeduperService
{
    private Func<string, string>? GetKey;
    private string[]? IgnoredSuffixes;
    private int? MinStringLength;
    private int? MaxDeviation;
    private readonly Dictionary<string, Dictionary<string, int>> _data = [];
    private readonly Dictionary<int, HashSet<string>> _keysByLength = [];

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

    public IStringDeduperBuilder UseLevenshtein(int minStringLength = 3, int maxDeviation = 1)
    {
        MinStringLength = minStringLength;
        MaxDeviation = maxDeviation;
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

            if (MinStringLength.HasValue && MaxDeviation.HasValue && MinStringLength.Value <= key.Length)
                key = CheckNeighbors(key, MaxDeviation.Value);

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

    private string CheckNeighbors(string key, int maxDeviation)
    {
        var neighbors = GetNeighbors(key.Length, maxDeviation);

        foreach (var neighbor in neighbors)
        {
            var distance = Levenshtein.GetDistance(key, neighbor);

            if (distance <= maxDeviation) return neighbor;
        }

        return key;
    }

    private IEnumerable<string> GetNeighbors(int length, int maxDeviation)
    {
        for (var i = Math.Max(1, length - maxDeviation); i <= length + maxDeviation; i++)
            if (_keysByLength.ContainsKey(i))
                foreach (var item in _keysByLength[i])
                    yield return item;
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

        if (_keysByLength.ContainsKey(key.Length))
        {
            if (!_keysByLength[key.Length].Contains(key))
            {
                _keysByLength[key.Length].Add(key);
            }
        }
        else
        {
            _keysByLength.Add(key.Length, [key]);
        }
    }
}
