using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LeynerLearner.Core;

/// <summary>
/// Data\manifest.json: each dataset folder of the app's data (where it came from, its version and license) and the
/// size and SHA-256 of every file in it. A data pack is a zip of a Data folder with its manifest at the top.
/// Regenerate the hashes with Tools\DataManifest.
/// </summary>
public sealed class DataManifest
{
    public const string FileName = "manifest.json";

    /// <summary>The newest manifest format this code reads.</summary>
    public const int CurrentFormat = 1;

    /// <summary>The folders the app reads; a pack must have all of them, so generated files stay consistent with their sources.</summary>
    public static readonly string[] RequiredDatasets = ["MAM", "PhoneticMAM", "Hebcal", "English", "Fonts"];

    public int Format { get; set; } = CurrentFormat;

    /// <summary>The pack's version, a date such as "2026.10.20"; packs compare by it (ordinal).</summary>
    public string PackVersion { get; set; } = "";

    public string? Description { get; set; }
    public List<Dataset> Datasets { get; set; } = [];

    public Dataset? this[string folder] => Datasets.FirstOrDefault(d => d.Folder.Equals(folder, StringComparison.OrdinalIgnoreCase));

    public static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
    };

    public static DataManifest Load(string dataDir) =>
        JsonSerializer.Deserialize<DataManifest>(File.ReadAllText(Path.Combine(dataDir, FileName)), Json)
        ?? throw new InvalidDataException("The manifest is empty.");

    /// <summary>The manifest of a data folder, or null if it has none or it cannot be read.</summary>
    public static DataManifest? TryLoad(string dataDir)
    {
        try { return Load(dataDir); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or InvalidDataException) { return null; }
    }

    public void Save(string dataDir) => File.WriteAllText(Path.Combine(dataDir, FileName), JsonSerializer.Serialize(this, Json) + "\n");

    /// <summary>
    /// Checks a data folder against this manifest: the format is one this code reads, every required dataset is listed,
    /// every listed file is there with the right size and hash, and there are no other files. Returns the problems found.
    /// </summary>
    public List<string> Verify(string dataDir, CancellationToken cancel = default)
    {
        var problems = new List<string>();
        if (Format > CurrentFormat) problems.Add($"Manifest format {Format} is newer than this version of the app reads ({CurrentFormat}).");
        foreach (var folder in RequiredDatasets.Where(f => this[f] == null)) problems.Add($"Dataset {folder} is missing.");

        var expected = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { FileName };
        foreach (var d in Datasets)
        {
            if (!IsSafeName(d.Folder)) { problems.Add($"Bad dataset folder name \"{d.Folder}\"."); continue; }
            foreach (var f in d.Files)
            {
                cancel.ThrowIfCancellationRequested();
                if (!IsSafePath(f.Path)) { problems.Add($"Bad file name \"{d.Folder}/{f.Path}\"."); continue; }
                string rel = d.Folder + "/" + f.Path;
                expected.Add(rel);
                var path = Path.Combine(dataDir, d.Folder, f.Path);
                var info = new FileInfo(path);
                if (!info.Exists) problems.Add($"{rel} is missing.");
                else if (info.Length != f.Size) problems.Add($"{rel} is {info.Length:N0} bytes, not {f.Size:N0}.");
                else if (!Hash(path).Equals(f.Sha256, StringComparison.OrdinalIgnoreCase)) problems.Add($"{rel} does not match its SHA-256.");
            }
        }
        foreach (var rel in AllFiles(dataDir).Where(r => !expected.Contains(r))) problems.Add($"{rel} is not in the manifest.");
        return problems;
    }

    /// <summary>Every file under a folder, as a relative path with "/" separators, in ordinal order.</summary>
    public static IEnumerable<string> AllFiles(string dir) =>
        Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories)
                 .Select(p => Path.GetRelativePath(dir, p).Replace('\\', '/'))
                 .Order(StringComparer.Ordinal);

    public static string Hash(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexStringLower(SHA256.HashData(stream));
    }

    static bool IsSafeName(string s) => s.Length > 0 && s.IndexOfAny(['/', '\\', ':']) < 0 && s != "." && s != "..";

    static bool IsSafePath(string s) => !Path.IsPathRooted(s) && s.Split('/').All(IsSafeName);
}

/// <summary>One folder of the data, from one upstream source.</summary>
public sealed class Dataset
{
    public string Folder { get; set; } = "";
    public string Name { get; set; } = "";
    public string Source { get; set; } = "";

    /// <summary>The upstream version: a commit, a package version or an edition.</summary>
    public string Version { get; set; } = "";

    /// <summary>When it was taken from upstream (yyyy-MM-dd), if known.</summary>
    public string? Date { get; set; }

    /// <summary>The license as an SPDX expression, where one exists.</summary>
    public string? Spdx { get; set; }

    /// <summary>The license in words.</summary>
    public string License { get; set; } = "";

    public string? Notes { get; set; }

    /// <summary>Files made by a tool in this repository rather than copied from upstream: file → how it is made.</summary>
    public Dictionary<string, string>? Generated { get; set; }

    public List<DataFile> Files { get; set; } = [];
}

public sealed class DataFile
{
    /// <summary>Relative to the dataset folder, with "/" separators.</summary>
    public string Path { get; set; } = "";
    public long Size { get; set; }
    public string Sha256 { get; set; } = "";
}
