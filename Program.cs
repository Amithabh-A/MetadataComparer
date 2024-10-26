using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

class Program
{
    static void Main(string[] args)
    {
        if (!ValidateArguments(args))
        {
            return;
        }

        string metadataFilePathA = args[0];
        string metadataFilePathB = args[1];

        if (!FilesExist(metadataFilePathA, metadataFilePathB))
        {
            return;
        }

        List<FileMetadata> metadataA = LoadMetadata(metadataFilePathA);
        List<FileMetadata> metadataB = LoadMetadata(metadataFilePathB);

        Dictionary<int, List<object>> differences = CompareMetadata(metadataA, metadataB);
        WriteDifferencesToFile(differences, "differences.json");
    }

    /// <summary>
    /// Checks whether necessary arguments are provided. 
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    /// <returns>True if the arguments are valid; otherwise, false.</returns>
    private static bool ValidateArguments(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("Usage: dotnet run <path-to-metadata.json-fA> <path-to-metadata.json-fB>");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Checks whether the files exist.
    /// </summary>
    /// <param name="pathA">Path of the first file.</param>
    /// <param name="pathB">Path of the second file.</param>
    /// <returns>True if the files exist; otherwise, false.</returns>
    private static bool FilesExist(string pathA, string pathB)
    {
        if (!File.Exists(pathA))
        {
            Console.WriteLine($"Metadata file does not exist: {pathA}");
            return false;
        }

        if (!File.Exists(pathB))
        {
            Console.WriteLine($"Metadata file does not exist: {pathB}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Loads metadata from specified metadatafile
    /// </summary>
    /// <param name="filePath">Path to the metadata file.</param>
    /// <returns>List of FileMetadata objects.</returns>
    private static List<FileMetadata> LoadMetadata(string filePath)
    {
        using FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        // if fileStream is null, metadata will be null. 
        List<FileMetadata> metadata = JsonSerializer.Deserialize<List<FileMetadata>>(fileStream);
        return metadata ?? new List<FileMetadata>();
    }

    /// <summary>
    /// Compares metadata from two <see cref="List<FileMetadata>"/> objects and returns the differences.
    /// </summary>
    /// <param name="metadataA">The first list of metadata.</param>
    /// <param name="metadataB">The second list of metadata.</param>
    /// <returns>A dictionary containing the differences between the two lists of metadata.</returns>
    private static Dictionary<int, List<object>> CompareMetadata(List<FileMetadata> metadataA, List<FileMetadata> metadataB)
    {
        Dictionary<int, List<object>> differences = new Dictionary<int, List<object>>
        {
            { -1, new List<object>() }, // In B but not in A
            { 0, new List<object>() },  // Files with same hash but different names
            { 1, new List<object>() }    // In A but not in B
        };

        Dictionary<string, string> hashToFileA = CreateHashToFileDictionary(metadataA);
        Dictionary<string, string> hashToFileB = CreateHashToFileDictionary(metadataB);

        CheckForRenamesAndMissingFiles(metadataB, hashToFileA, differences);
        CheckForOnlyInAFiles(metadataA, hashToFileB, differences);

        return differences;
    }

    /// <summary>
    /// Creates a dictionary that maps file hashes to file names.
    /// </summary>
    /// <param name="metadata">The list of metadata.</param>
    /// <returns>A dictionary that maps file hashes to file names.</returns>
    private static Dictionary<string, string> CreateHashToFileDictionary(List<FileMetadata> metadata)
    {
        var hashToFile = new Dictionary<string, string>();
        foreach (var file in metadata)
        {
            hashToFile[file.FileHash] = file.FileName;
        }
        return hashToFile;
    }

    /// <summary>
    /// Checks for files in directory B that have been renamed or missing in directory A.
    /// </summary>
    /// <param name="metadataB">The list of metadata for directory B.</param>
    /// <param name="hashToFileA">Dictionary that maps file hashes to file names in directory A.</param>
    /// <param name="differences">The dictionary that stores the differences.</param>
    /// <returns> The dictionary that stores the differences.</returns>
    private static void CheckForRenamesAndMissingFiles(List<FileMetadata> metadataB, Dictionary<string, string> hashToFileA, Dictionary<int, List<object>> differences)
    {
        foreach (FileMetadata fileB in metadataB)
        {
            if (hashToFileA.ContainsKey(fileB.FileHash))
            {
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
    }

    /// <summary>
    /// Checks for files in directory A that are missing in directory B.
    /// </summary>
    /// <param name="metadataA">The list of metadata for directory A.</param>
    /// <param name="hashToFileB">Dictionary that maps file hashes to file names in directory B.</param>
    /// <param name="differences">The dictionary that stores the differences.</param>
    /// <returns> The dictionary that stores the differences.</returns>
    private static void CheckForOnlyInAFiles(List<FileMetadata> metadataA, Dictionary<string, string> hashToFileB, Dictionary<int, List<object>> differences)
    {
        foreach (FileMetadata fileA in metadataA)
        {
            if (!hashToFileB.ContainsKey(fileA.FileHash))
            {
                differences[1].Add(new Dictionary<string, string>
                {
                    { "FileName", fileA.FileName },
                    { "FileHash", fileA.FileHash }
                });
            }
        }
    }

    /// <summary>
    /// </summary>
    /// <param name="differences"></param>
    /// <param name="outputFilePath"></param>
    /// <returns></returns>
    private static void WriteDifferencesToFile(Dictionary<int, List<object>> differences, string outputFilePath)
    {
        JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(outputFilePath, JsonSerializer.Serialize(differences, options));
        Console.WriteLine($"Differences written to: {outputFilePath}");
    }
}

/// <summary>
/// Represents metadata for a file, including its name and hash.
/// </summary>
class FileMetadata
{
    public string FileName { get; set; }
    public string FileHash { get; set; }
}

