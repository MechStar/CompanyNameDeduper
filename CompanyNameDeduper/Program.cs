using System.Text.RegularExpressions;
using StringDeduper;
using StringDeduper.Helpers;

var companyNameDeduper = new StringDeduperBuilder()
    .AddNormalizeStrategy(str => MyPattern().Replace(str.ToLowerInvariant(), string.Empty))
    .AddIgnoredSuffixes(
    [
        "org",
        "net",
        "mil",
        "ltd",
        "llc",
        "limitedliabilitycompany",
        "limited",
        "int",
        "incorporated",
        "inc",
        "gov",
        "edu",
        "corporation",
        "corp",
        "company",
        "com",
        "co",
    ])
    .UseLevenshtein()
    .Build();

using var httpClient = new HttpClient();
using var stream = await httpClient.GetStreamAsync("https://s3.amazonaws.com/ym-hosting/tomtest/advertisers.txt");

await companyNameDeduper.ImportStrings(HttpUtility.ReadAsync(stream));
await FileUtility.WriteAsync("output.txt", companyNameDeduper.GetDuplicates(true));

partial class Program
{
    [GeneratedRegex("[^a-z0-9]")]
    private static partial Regex MyPattern();
}
