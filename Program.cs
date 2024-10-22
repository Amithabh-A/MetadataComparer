using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

class Program
{
    /// <summary>
    /// Entry point of the application. Compares two metadata JSON files and outputs their differences.
    /// </summary>
    /// <param name="args">Command line arguments for the paths to the metadata files.</param>
    static void Main(string[] args)
    {
        // Check if the correct number of arguments is provided
        if (args.Length != 2)
        {
            Console.WriteLine("Usage: dotnet run <path-to-metadata.json-fA> <path-to-metadata.json-fB>");
            return;
        }

        string metadataFilePathA = args[0]; // Path to the first metadata file (fA)
        string metadataFilePathB = args[1]; // Path to the second metadata file (fB)

        // Check if the first metadata file exists
        if (!File.Exists(metadataFilePathA))
        {
            Console.WriteLine($"Metadata file does not exist: {metadataFilePathA}");
            return;
        }

        // Check if the second metadata file exists
        if (!File.Exists(metadataFilePathB))
        {
            Console.WriteLine($"Metadata file does not exist: {metadataFilePathB}");
            return;
        }

        var metadataA = LoadMetadata(metadataFilePathA);
        var metadataB = LoadMetadata(metadataFilePathB);

        // Initialize the differences dictionary
        var differences = new Dictionary<int, List<object>>
        {
            { -1, new List<object>() }, // In B but not in A
            { 0, new List<object>() },  // Files with same hash but different names
            { 1, new List<object>() }    // In A but not in B
        };

        // Create hash maps for fast lookup of filenames by their hash
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

        // Compare metadata from both directories
        foreach (var fileB in metadataB)
        {
            if (hashToFileA.ContainsKey(fileB.FileHash))
            {
                // Check if file hashes are the same but filenames are different
                if (hashToFileA[fileB.FileHash] != fileB.FileName)
                {
                    // Add rename information to the differences dictionary
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
                // File exists in B but not in A
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
                // Only in A
                differences[1].Add(new Dictionary<string, string>
                {
                    { "FileName", fileA.FileName },
                    { "FileHash", fileA.FileHash }
                });
            }
        }

        // Specify the output file path for differences
        string outputFilePath = "differences.json";
        var options = new JsonSerializerOptions { WriteIndented = true }; // For pretty JSON output
        File.WriteAllText(outputFilePath, JsonSerializer.Serialize(differences, options)); // Write to file

        Console.WriteLine($"Differences written to: {outputFilePath}"); // Confirm output
    }

    /// <summary>
    /// Loads metadata from a specified JSON file.
    /// </summary>
    /// <param name="filePath">Path to the metadata file.</param>
    /// <returns>List of FileMetadata objects.</returns>
    private static List<FileMetadata> LoadMetadata(string filePath)
    {
        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        return JsonSerializer.Deserialize<List<FileMetadata>>(fileStream); // Deserialize JSON to List<FileMetadata>
    }
}

/// <summary>
/// Represents metadata for a file, including its name and hash.
/// </summary>
class FileMetadata
{
    public string FileName { get; set; } // File name
    public string FileHash { get; set; }  // File hash
}

