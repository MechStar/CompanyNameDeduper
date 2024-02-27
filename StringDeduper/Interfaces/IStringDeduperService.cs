namespace StringDeduper.Interfaces;

public interface IStringDeduperService
{
    Task ImportStrings(IAsyncEnumerable<string> input);
    IEnumerable<string> GetDuplicates(bool includeOriginal = false, bool excludeRepeats = false, bool addEmptyString = false);
    IEnumerable<string> GetUniques(bool restrictToUniqueInput = false);
    void ClearMemory();
}
