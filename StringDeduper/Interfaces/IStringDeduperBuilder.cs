namespace StringDeduper.Interfaces;

public interface IStringDeduperBuilder
{
    IStringDeduperBuilder AddNormalizeStrategy(Func<string, string> getKey);
    IStringDeduperBuilder AddIgnoredSuffixes(string[]? ignoredSuffixes = default);
    IStringDeduperService Build();
}
