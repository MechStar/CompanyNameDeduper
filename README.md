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

deduper.ClearMemory();
```
