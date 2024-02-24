using Quickenshtein;
using StringDeduper.Enums;
using StringDeduper.Helpers;
using StringDeduper.Interfaces;

namespace StringDeduper;

public class StringDeduperBuilder : IStringDeduperBuilder, IStringDeduperService
{
    private Func<string, string>? GetKey;
    private string[]? IgnoredSuffixes;
    private FuzzyMatchingStrategy? Strategy;
    private int MinStringLength;
    private int MaxDeviation;
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

    public IStringDeduperBuilder UseFuzzyMatching(FuzzyMatchingStrategy strategy = FuzzyMatchingStrategy.Levenshtein, int minStringLength = 5, int maxDeviation = 1)
    {
        Strategy = strategy;
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

            if (Strategy != null && MinStringLength <= key.Length)
                key = CheckNeighbors(key, MaxDeviation, Strategy.Value);

            SaveStringByKey(key, str, Strategy != null);
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

    private string CheckNeighbors(string key, int maxDeviation, FuzzyMatchingStrategy strategy)
    {
        var neighbors = GetNeighbors(key.Length, maxDeviation);

        Func<string, string, int, bool> isFuzzyMatch = strategy switch
        {
            FuzzyMatchingStrategy.Bitap => IsBitapMatch,
            FuzzyMatchingStrategy.Levenshtein => IsLevenshteinMatch,
            _ => throw new NotImplementedException()
        };

        foreach (var neighbor in neighbors)
            if (isFuzzyMatch(key, neighbor, maxDeviation))
                return neighbor;

        return key;
    }

    private IEnumerable<string> GetNeighbors(int length, int maxDeviation)
    {
        for (var i = Math.Max(1, length - maxDeviation); i <= length + maxDeviation; i++)
            if (_keysByLength.TryGetValue(i, out var values))
                foreach (var value in values)
                    yield return value;
    }

    private bool IsBitapMatch(string str1, string str2, int maxDeviation)
    {
        string[] pair = [str1, str2];
        pair = [.. pair.OrderByDescending(x => x.Length)];

        var index = StringUtility.SearchString(pair.First(), pair.Last(), maxDeviation);

        return index > -1;
    }

    private bool IsLevenshteinMatch(string str1, string str2, int maxDeviation)
    {
        var distance = Levenshtein.GetDistance(str1, str2, CalculationOptions.DefaultWithThreading);

        return distance <= maxDeviation;
    }

    private void SaveStringByKey(string key, string value, bool saveKeyByLength)
    {
        if (_data.TryGetValue(key, out var variations))
        {
            if (variations.TryGetValue(value, out _))
            {
                variations[value]++;
            }
            else
            {
                variations.Add(value, 1);
            }
        }
        else
        {
            _data.Add(key, new Dictionary<string, int> { { value, 1 } });
        }

        if (saveKeyByLength) SaveKeyByLength(key.Length, key);
    }

    private void SaveKeyByLength(int key, string value)
    {
        if (_keysByLength.TryGetValue(key, out var values))
        {
            values.Add(value);
        }
        else
        {
            _keysByLength.Add(key, [value]);
        }
    }
}
