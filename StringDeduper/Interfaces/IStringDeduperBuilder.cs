using StringDeduper.Enums;

namespace StringDeduper.Interfaces;

public interface IStringDeduperBuilder
{
    IStringDeduperBuilder AddNormalizeStrategy(Func<string, string> getKey);
    IStringDeduperBuilder AddIgnoredSuffixes(string[]? ignoredSuffixes = default);
    IStringDeduperBuilder UseFuzzyMatching(FuzzyMatchingStrategy strategy = FuzzyMatchingStrategy.Levenshtein, int minStringLength = 5, int maxDeviation = 1);
    IStringDeduperService Build();
}
