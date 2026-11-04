using ProductRAGDemo.Models;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace ProductRAGDemo.Services;

public static class KnowledgeLoader
{
    public static List<KnowledgeDocument> LoadDocuments(
        string folder)
    {
        var documents =
            new List<KnowledgeDocument>();

        if (!Directory.Exists(folder))
        {
            throw new DirectoryNotFoundException(
                $"Folder not found : {folder}");
        }

        var files =
            Directory
                .GetFiles(
                    folder,
                    "*.*",
                    SearchOption.AllDirectories)
                .Where(x =>
                    x.EndsWith(".txt",
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    x.EndsWith(".pdf",
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

        Console.WriteLine(
            $"Files Found: {files.Count}");

        foreach (var file in files)
        {
            Console.WriteLine(
                $"Loading: {Path.GetFileName(file)}");

            documents.Add(
                new KnowledgeDocument
                {
                    FileName = Path.GetFileName(file),
                    FileHash = GetHash(file),
                    Content = ReadContent(file)
                });
        }

        return documents;
    }

    private static string ReadContent(
        string filePath)
    {
        if (filePath.EndsWith(
            ".txt",
            StringComparison.OrdinalIgnoreCase))
        {
            return File.ReadAllText(filePath);
        }

        using var pdf =
            PdfDocument.Open(filePath);

        var text = "";

        foreach (var page in pdf.GetPages())
        {
            text += page.Text;
            text += Environment.NewLine;
        }

        text = Regex.Replace(
            text,
            @"\s+",
            " ");

        return text;
    }

    private static string GetHash(
    string filePath)
    {
        using var sha =
            SHA256.Create();

        var bytes =
            File.ReadAllBytes(filePath);

        var hash =
            sha.ComputeHash(bytes);

        return Convert.ToHexString(hash);
    }
}