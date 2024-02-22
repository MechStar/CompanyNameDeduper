# CompanyNameDeduper

## Usage
```csharp
var deduper = new CompanyNameDeduper();
await deduper.ReadFromFileAsync("input.txt");

// write dups to file (default behavior)
await deduper.WriteToFileAsync("output.txt");

// write uniques to file
await deduper.WriteToFileAsync("outputUniques.txt", ExportType.Uniques);

// write all to file
await deduper.WriteToFileAsync("outputUniques.txt", ExportType.All);

// write companies with "google" or "hugo" in the name
await deduper.WriteToFileAsync("outputGoogleOrHugo1.txt", x => x.Key.Contains("google") || x.Key.Contains("hugo"));

// write companies with "google" or "hugo" in the name skipping literal dups
await deduper.WriteToFileAsync("outputGoogleOrHugo2.txt", x => x.Key.Contains("google") || x.Key.Contains("hugo"), true);

// write companies with "google" or "hugo" in the name skipping literal dups and skipping a line between groups
await deduper.WriteToFileAsync("outputGoogleOrHugo3.txt", x => x.Key.Contains("google") || x.Key.Contains("hugo"), true, true);

deduper.ClearMemory();
```
