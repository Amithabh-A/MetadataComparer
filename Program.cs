using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("Usage: dotnet run <path-to-metadata.json-fA> <path-to-metadata.json-fB>");
            return;
        }

        string metadataFilePathA = args[0];
        string metadataFilePathB = args[1];

        if (!File.Exists(metadataFilePathA))
        {
            Console.WriteLine($"Metadata file does not exist: {metadataFilePathA}");
            return;
        }

        if (!File.Exists(metadataFilePathB))
        {
            Console.WriteLine($"Metadata file does not exist: {metadataFilePathB}");
            return;
        }

        var metadataA = LoadMetadata(metadataFilePathA);
        var metadataB = LoadMetadata(metadataFilePathB);

        // Initialize differences dictionary
        var differences = new Dictionary<int, List<object>>
        {
            { -1, new List<object>() }, // In B but not in A
            { 0, new List<object>() },  // Files with same hash but different names
            { 1, new List<object>() }    // In A but not in B
        };

        // Create hash maps for fast lookup
        var hashToFileA = new Dictionary<string, string>(); // Hash to filename in A
        var hashToFileB = new Dictionary<string, string>(); // Hash to filename in B

        foreach (var fileA in metadataA)
        {
            hashToFileA[fileA.FileHash] = fileA.FileName;
        }

        foreach (var fileB in metadataB)
        {
            hashToFileB[fileB.FileHash] = fileB.FileName;
        }

        // Compare metadata
        foreach (var fileB in metadataB)
        {
            if (hashToFileA.ContainsKey(fileB.FileHash))
            {
                // File hashes are the same but file names are different
                if (hashToFileA[fileB.FileHash] != fileB.FileName)
                {
                    differences[0].Add(new Dictionary<string, string>
                    {
                        { "RenameFrom", fileB.FileName },
                        { "RenameTo", hashToFileA[fileB.FileHash] },
                        { "FileHash", fileB.FileHash }
                    });
                }
            }
            else
            {
                differences[-1].Add(new Dictionary<string, string>
                {
                    { "FileName", fileB.FileName },
                    { "FileHash", fileB.FileHash }
                });
            }
        }

        foreach (var fileA in metadataA)
        {
            if (!hashToFileB.ContainsKey(fileA.FileHash))
            {
                differences[1].Add(new Dictionary<string, string>
                {
                    { "FileName", fileA.FileName },
                    { "FileHash", fileA.FileHash }
                }); // Add as dictionary for consistency
            }
        }

        string outputFilePath = "differences.json";
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(outputFilePath, JsonSerializer.Serialize(differences, options));

        Console.WriteLine($"Differences written to: {outputFilePath}");
    }

    private static List<FileMetadata> LoadMetadata(string filePath)
    {
        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        return JsonSerializer.Deserialize<List<FileMetadata>>(fileStream);
    }
}

class FileMetadata
{
    public string FileName { get; set; }
    public string FileHash { get; set; }
}

