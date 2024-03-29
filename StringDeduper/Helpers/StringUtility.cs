namespace StringDeduper.Helpers;

public static class StringUtility
{
    // Source: https://www.programmingalgorithms.com/algorithm/fuzzy-bitap-algorithm
    public static int SearchString(string text, string pattern, int k)
    {
        int result = -1;
        int m = pattern.Length;
        int[] R;
        int[] patternMask = new int[128];
        int i, d;

        if (string.IsNullOrEmpty(pattern)) return 0;
        if (m > 31) return -1;

        R = new int[(k + 1) * sizeof(int)];
        for (i = 0; i <= k; ++i)
            R[i] = ~1;

        for (i = 0; i <= 127; ++i)
            patternMask[i] = ~0;

        for (i = 0; i < m; ++i)
            patternMask[pattern[i]] &= ~(1 << i);

        for (i = 0; i < text.Length; ++i)
        {
            int oldRd1 = R[0];

            R[0] |= patternMask[text[i]];
            R[0] <<= 1;

            for (d = 1; d <= k; ++d)
            {
                int tmp = R[d];

                R[d] = (oldRd1 & (R[d] | patternMask[text[i]])) << 1;
                oldRd1 = tmp;
            }

            if (0 == (R[k] & (1 << m)))
            {
                result = i - m + 1;
                break;
            }
        }

        return result;
    }
}
