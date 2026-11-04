namespace ProductRAGDemo.Services;

public static class SearchService
{
    public static string Search(
        List<string> chunks,
        string question)
    {
        var keywords =
            question
                .ToLower()
                .Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries);

        var results =
            chunks
                .Select(chunk => new
                {
                    Chunk = chunk,

                    Score =
                        keywords.Count(word =>
                            chunk.Contains(
                                word,
                                StringComparison.OrdinalIgnoreCase))
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Take(3)
                .ToList();

        if (!results.Any())
        {
            return "No relevant information found.";
        }

        return string.Join(
            Environment.NewLine +
            Environment.NewLine +
            "==================================" +
            Environment.NewLine +
            Environment.NewLine,
            results.Select(x => x.Chunk));
    }
}