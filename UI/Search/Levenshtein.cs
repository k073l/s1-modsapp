namespace ModsApp.UI.Search;

public static class Levenshtein
{
    public static int Distance(string a, string b)
    {
        if (string.IsNullOrEmpty(a)) return b?.Length ?? 0;
        if (string.IsNullOrEmpty(b)) return a.Length;

        var aLen = a.Length;
        var bLen = b.Length;

        var distances = new int[aLen + 1, bLen + 1];

        for (var i = 0; i <= aLen; i++) distances[i, 0] = i;
        for (var j = 0; j <= bLen; j++) distances[0, j] = j;

        for (var i = 1; i <= aLen; i++)
        {
            for (var j = 1; j <= bLen; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                distances[i, j] = Math.Min(
                    Math.Min(distances[i - 1, j] + 1, distances[i, j - 1] + 1),
                    distances[i - 1, j - 1] + cost);
            }
        }

        return distances[aLen, bLen];
    }

    public static float Similarity(string a, string b)
    {
        if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b)) return 1f;
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0f;

        var distance = Distance(a, b);
        var maxLen = Math.Max(a.Length, b.Length);
        return 1f - (float)distance / maxLen;
    }
}