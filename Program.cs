using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

class Program
{
  /// <summary>
  /// Compares two metadata JSON files and outputs their differences.
  /// uses JsonSerializer to serialize object to JSON data and deserialize from JSON data to object.
  /// </summary>
  /// <param name="args">command line arguments.</param>
  static void Main(string[] args)
  {
    // Check if the correct number of arguments is provided
    if (args.Length != 2)
    {
      Console.WriteLine("Usage: dotnet run <path-to-metadata.json-fA> <path-to-metadata.json-fB>");
      return;
    }

    string metadataFilePathA = args[0]; // Path to file 1
    string metadataFilePathB = args[1]; // Path to file 2

    // Check if file 1 exists
    if (!File.Exists(metadataFilePathA))
    {
      Console.WriteLine($"Metadata file does not exist: {metadataFilePathA}");
      return;
    }

    // Check if file 2 exists
    if (!File.Exists(metadataFilePathB))
    {
      Console.WriteLine($"Metadata file does not exist: {metadataFilePathB}");
      return;
    }

    var metadataA = LoadMetadata(metadataFilePathA);
    var metadataB = LoadMetadata(metadataFilePathB);

    // Initialize the differences dictionary
    // values of dictionary can be list of anything. 
    // object is base of all types in c#
    var differences = new Dictionary<int, List<object>>
        {
            { -1, new List<object>() }, // In B but not in A
            { 0, new List<object>() },  // Files with same hash but different names
            { 1, new List<object>() }    // In A but not in B
        };

    // Converting metadata of files to dictionaries
    var hashToFileA = new Dictionary<string, string>();
    var hashToFileB = new Dictionary<string, string>();

    foreach (var fileA in metadataA)
    {
      hashToFileA[fileA.FileHash] = fileA.FileName;
    }

    foreach (var fileB in metadataB)
    {
      hashToFileB[fileB.FileHash] = fileB.FileName;
    }

    // <=> fileData creation
    foreach (var fileB in metadataB)
    {
      if (hashToFileA.ContainsKey(fileB.FileHash))
      {
        // Check if file hashes are same but filenames are different
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
    // File.WriteAllText(path, content)
    File.WriteAllText(outputFilePath, JsonSerializer.Serialize(differences, options)); // Write to file

    Console.WriteLine($"Differences written to: {outputFilePath}"); // Confirm output
  }

  /// <summary>
  /// Loads metadata from a specified JSON file by deserializing using JsonSerializer.deserialize()
  /// </summary>
  /// <param name="filePath">Path to the metadata file.</param>
  /// <returns>List of FileMetadata objects.</returns>
  private static List<FileMetadata> LoadMetadata(string filePath)
  {
    using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
    return JsonSerializer.Deserialize<List<FileMetadata>>(fileStream);
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

