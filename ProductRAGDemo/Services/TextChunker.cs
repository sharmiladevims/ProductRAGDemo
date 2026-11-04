namespace ProductRAGDemo.Services;

public static class TextChunker
{
    public static List<string> Chunk(
        string document,
        int chunkSize = 500)
    {
        var chunks =
            new List<string>();

        for (var i = 0;
             i < document.Length;
             i += chunkSize)
        {
            chunks.Add(
                document.Substring(
                    i,
                    Math.Min(
                        chunkSize,
                        document.Length - i)));
        }

        return chunks;
    }
}
