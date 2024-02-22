var deduper = new CompanyNameDeduper();
await deduper.ReadFromFileAsync("input.txt");
await deduper.WriteToFileAsync("output.txt");
