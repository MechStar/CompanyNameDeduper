# CompanyNameDeduper

## Basic Usage
```csharp
using StringDeduper;
using StringDeduper.Helpers;

var deduper = new StringDeduperBuilder()
    .Build();

await deduper.ImportStrings(FileUtility.ReadAsync("input.txt"));
await FileUtility.WriteAsync("output.txt", deduper.GetDuplicates());
```

## Advanced Usage
```csharp
using System.Text.RegularExpressions;
using StringDeduper;
using StringDeduper.Helpers;

var companyNameDeduper = new StringDeduperBuilder()
    .AddNormalizeStrategy(str => MyPattern().Replace(str.ToLowerInvariant(), string.Empty))
    .AddIgnoredSuffixes(
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
    ])
    .Build();

await companyNameDeduper.ImportStrings(FileUtility.ReadAsync("input.txt"));
await FileUtility.WriteAsync("output.txt", companyNameDeduper.GetDuplicates(true));

partial class Program
{
    [GeneratedRegex("[^a-z0-9]")]
    private static partial Regex MyPattern();
}
```
