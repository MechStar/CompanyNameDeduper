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

    /// <summary>
    /// Builder method that adds function for generating key from input string (otherwise input string itself is key)
    /// </summary>
    /// <param name="getKey">Function that accepts string input and returns string key</param>
    /// <returns>IStringDeduperBuilder</returns>
    public IStringDeduperBuilder AddNormalizeStrategy(Func<string, string> getKey)
    {
        GetKey = getKey;

        return this;
    }

    /// <summary>
    /// Builder method that adds ignored suffixes to be removed during key generation (order of removal is order or array)
    /// </summary>
    /// <param name="ignoredSuffixes">Array of string suffixes in order of removal</param>
    /// <returns>IStringDeduperBuilder</returns>
    public IStringDeduperBuilder AddIgnoredSuffixes(string[] ignoredSuffixes)
    {
        IgnoredSuffixes = ignoredSuffixes;

        return this;
    }

    /// <summary>
    /// Builder method that adds fuzzy matching strategy to be used for deduplication, which changes import complexity from O(n) to O(n^2)
    /// </summary>
    /// <param name="strategy">Levenshtein strategy is default, but Bitap strategy can be used instead</param>
    /// <param name="minStringLength">Minimum string length of resulting key to qualify for fuzzy matching (minimum value is 1)</param>
    /// <param name="maxDeviation">Maximum distance between strings or deviation in length (minimum value is 1)</param>
    /// <returns>IStringDeduperBuilder</returns>
    public IStringDeduperBuilder UseFuzzyMatching(FuzzyMatchingStrategy strategy = FuzzyMatchingStrategy.Levenshtein, int minStringLength = 5, int maxDeviation = 1)
    {
        if (minStringLength < 1) throw new ArgumentOutOfRangeException(nameof(minStringLength), "Must be greater than or equals to 1.");
        if (maxDeviation < 1) throw new ArgumentOutOfRangeException(nameof(maxDeviation), "Must be greater than or equals to 1.");

        Strategy = strategy;
        MinStringLength = minStringLength;
        MaxDeviation = maxDeviation;

        return this;
    }

    /// <summary>
    /// Builder method that returns constructed deduper service
    /// </summary>
    /// <returns>IStringDeduperService</returns>
    public IStringDeduperService Build()
    {
        // apply same normalize strategy to ignored suffixes at build time
        IgnoredSuffixes = IgnoredSuffixes?.Select(x => GetKey is not null ? GetKey(x) : x).OrderByDescending(x => x).ToArray();

        return this;
    }

    /// <summary>
    /// Reads IAsyncEnumerable of input strings (defer execution until enumeration), normalizes them, and stores them in memory
    /// </summary>
    /// <param name="input">There are helper methods to get IAsyncEnumerable from file or HTTP</param>
    /// <returns>Task</returns>
    public async Task ImportStrings(IAsyncEnumerable<string> input)
    {
        await foreach (var str in input)
        {
            // if specified, use normalize strategy to generate key
            var key = GetKey is not null ? GetKey(str) : str;

            // if specified, loop through suffixes to remove ignored
            if (IgnoredSuffixes is not null)
                foreach (var suffix in IgnoredSuffixes)
                    key = key.EndsWith(suffix) ? key[..^suffix.Length] : key;

            // if specified, use fuzzy matching strategy to improve key
            if (Strategy is not null && MinStringLength <= key.Length)
                key = CheckNeighbors(key, MaxDeviation, Strategy.Value);

            // save key/value, and optionally length/key for fuzzy matching
            SaveStringByKey(key, str, Strategy is not null);
        }
    }

    /// <summary>
    /// Applies filter to normalized data in memory to return duplicates as IEnumerable (defer execution until enumeration)
    /// </summary>
    /// <param name="includeOriginal">Include first instance of string in matched group (technically not part of duplicates), default is False</param>
    /// <param name="excludeRepeats">Exclude literal repeats, i.e.: "Microsoft" and "Microsoft", from duplicate list, default is False</param>
    /// <param name="addEmptyString">Add empty element between each group of potential duplicates to serve as group delimiter</param>
    /// <returns>Returns list of strings as IEnumerable to defer execution until enumeration</returns>
    public IEnumerable<string> GetDuplicates(bool includeOriginal = false, bool excludeRepeats = false, bool addEmptyString = false)
    {
        // filter out uniques and leave only duplicates (having literal duplicates or multiple variations)
        var data = _data.Where(x => x.Value.First().Value > 1 || x.Value.Count > 1);

        foreach (var item in data)
        {
            var variations = item.Value;

            for (var i = 0; i < variations.Count; i++)
            {
                var variationFrequency = variations.ElementAt(i).Value;

                for (var j = 0; j < variationFrequency; j++)
                {
                    // if include first
                    if (!includeOriginal && i == 0 && j == 0) continue;

                    yield return variations.ElementAt(i).Key;

                    // if skip repeats
                    if (excludeRepeats) break;
                }
            }

            // if add delimiter
            if (addEmptyString) yield return string.Empty;
        }
    }

    /// <summary>
    /// Applies filter to normalized data in memory to return uniques as IEnumerable (defer execution until enumeration)
    /// </summary>
    /// <param name="restrictToUniqueInput">Restricts to items already unique in original input, as opposed to deduplicated uniques, default is False</param>
    /// <returns>Returns list of strings as IEnumerable to defer execution until enumeration</returns>
    public IEnumerable<string> GetUniques(bool restrictToUniqueInput = false)
    {
        // filter out duplicates and leave only uniques (optionally restrict to items already unique on input)
        var data = _data.Where(x => !restrictToUniqueInput || (x.Value.First().Value == 1 && x.Value.Count == 1));

        foreach (var item in _data)
            yield return item.Value.First().Key;
    }

    /// <summary>
    /// Checks other keys of length within maximum specified deviation, i.e.: if key length is 5 and maximum deviation is 1, check other keys of length 4, 5 and 6
    /// </summary>
    /// <param name="key">Normalized key string</param>
    /// <param name="maxDeviation">Maximum distance between strings or deviation in length</param>
    /// <param name="strategy">Fuzzy matching strategy defined during build</param>
    /// <returns>Returns key unchanged if no fuzzy match, otherwise returns first matched neighbor key</returns>
    /// <exception cref="NotImplementedException"></exception>
    private string CheckNeighbors(string key, int maxDeviation, FuzzyMatchingStrategy strategy)
    {
        var neighbors = GetNeighbors(key.Length, maxDeviation);

        // change strategy based on specified parameter
        Func<string, string, int, bool> IsFuzzyMatch = strategy switch
        {
            FuzzyMatchingStrategy.Bitap => IsBitapMatch,
            FuzzyMatchingStrategy.Levenshtein => IsLevenshteinMatch,
            _ => throw new NotImplementedException()
        };

        // if fuzzy matched to existing key, use that instead
        foreach (var neighbor in neighbors)
            if (IsFuzzyMatch(key, neighbor, maxDeviation))
                return neighbor;

        return key;
    }

    /// <summary>
    /// Gets already processed keys within maximum specified deviation, i.e.: if key length is 5 and maximum deviation is 1, get other keys of length 4, 5 and 6
    /// </summary>
    /// <param name="length">Length of key string to check against others</param>
    /// <param name="maxDeviation">Maximum distance between strings or deviation in length</param>
    /// <returns>Returns list of neighbors as IEnumerable to defer execution until enumeration</returns>
    private IEnumerable<string> GetNeighbors(int length, int maxDeviation)
    {
        for (var i = Math.Max(1, length - maxDeviation); i <= length + maxDeviation; i++)
            if (_keysByLength.TryGetValue(i, out var values))
                foreach (var value in values)
                    yield return value;
    }

    /// <summary>
    /// Attempts to fuzzy match two strings using Bitap algorithm (bitwise operations)
    /// </summary>
    /// <param name="str1">First string</param>
    /// <param name="str2">Second string</param>
    /// <param name="maxDeviation">Maximum distance between strings or deviation in length</param>
    /// <returns>True if matched, otherwise False</returns>
    private bool IsBitapMatch(string str1, string str2, int maxDeviation)
    {
        // for this algorithm the longer string should be first
        string[] pair = [str1, str2];
        pair = [.. pair.OrderByDescending(x => x.Length)];

        var index = StringUtility.SearchString(pair.First(), pair.Last(), maxDeviation);

        return index > -1;
    }

    /// <summary>
    /// Attempts to fuzzy match two strings using Levenshtein algorithm (minimum replacements)
    /// </summary>
    /// <param name="str1">First string</param>
    /// <param name="str2">Second string</param>
    /// <param name="maxDeviation">Maximum distance between strings or deviation in length</param>
    /// <returns>True if matched, otherwise False</returns>
    private bool IsLevenshteinMatch(string str1, string str2, int maxDeviation)
    {
        var distance = Levenshtein.GetDistance(str1, str2, CalculationOptions.DefaultWithThreading);

        return distance <= maxDeviation;
    }

    /// <summary>
    /// Saves normalized key with original value, and optionally saves key length with corresponding key
    /// </summary>
    /// <param name="key">Normalized key string</param>
    /// <param name="value">Original input value</param>
    /// <param name="saveKeyByLength">Set to save by length (needed for fuzzy matching)</param>
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

        // only needed to optimize fuzzy matching
        if (saveKeyByLength) SaveKeyByLength(key.Length, key);
    }

    /// <summary>
    /// Saves key length with corresponding key
    /// </summary>
    /// <param name="key">Normalized key length</param>
    /// <param name="value">Normalized key string</param>
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

    /// <summary>
    /// Clears internal data structures to prepare for new import/dedup/export
    /// </summary>
    public void ClearMemory()
    {
        _data.Clear();
        _keysByLength.Clear();
    }
}
