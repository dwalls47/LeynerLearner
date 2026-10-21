// Keeps LeynerLearner\Data\manifest.json up to date and builds data packs (a zip of the Data folder) for the app's
// "Update data" command. The manifest's dataset descriptions are written by hand; this tool fills in each dataset's
// file list (every file in its folder, with size and SHA-256).
using System.IO.Compression;
using LeynerLearner.Core;

// usage: DataManifest [update] [--pack-version <yyyy.MM.dd>] [--data <app Data dir>]
//        DataManifest check [--data <dir>]
//        DataManifest pack <out.zip> [--data <dir>]
string data = Path.Combine("..", "..", "LeynerLearner", "Data");
string? packVersion = null;
var rest = new List<string>();
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--data" && i + 1 < args.Length) data = args[++i];
    else if (args[i] == "--pack-version" && i + 1 < args.Length) packVersion = args[++i];
    else rest.Add(args[i]);
}
string command = rest.Count > 0 ? rest[0] : "update";

var manifest = DataManifest.Load(data);
switch (command)
{
    case "update":
    {
        if (packVersion != null) manifest.PackVersion = packVersion;
        foreach (var d in manifest.Datasets)
        {
            string dir = Path.Combine(data, d.Folder);
            if (!Directory.Exists(dir)) { Console.Error.WriteLine($"{d.Folder}: folder not found"); return 1; }
            var old = d.Files.ToDictionary(f => f.Path, f => f.Sha256);
            d.Files = DataManifest.AllFiles(dir)
                .Select(rel => new DataFile { Path = rel, Size = new FileInfo(Path.Combine(dir, rel)).Length, Sha256 = DataManifest.Hash(Path.Combine(dir, rel)) })
                .ToList();
            int changed = d.Files.Count(f => !old.TryGetValue(f.Path, out var h) || h != f.Sha256);
            int removed = old.Keys.Count(p => d.Files.All(f => f.Path != p));
            Console.WriteLine($"{d.Folder,-12} {d.Files.Count,4} files, {d.Files.Sum(f => f.Size),12:N0} bytes" +
                              (changed + removed > 0 ? $"  ({changed} new or changed, {removed} removed)" : ""));
        }
        manifest.Save(data);
        Warn(manifest, data);
        Console.WriteLine($"Wrote {Path.Combine(data, DataManifest.FileName)} (pack version {manifest.PackVersion}).");
        return 0;
    }
    case "check":
        return Check(manifest, data) ? 0 : 1;
    case "pack":
    {
        if (rest.Count < 2) { Console.Error.WriteLine("usage: DataManifest pack <out.zip> [--data <dir>]"); return 1; }
        if (!Check(manifest, data)) { Console.Error.WriteLine("Not packed; run DataManifest update first."); return 1; }
        string zip = Path.GetFullPath(rest[1]);
        File.Delete(zip);
        ZipFile.CreateFromDirectory(data, zip, CompressionLevel.SmallestSize, includeBaseDirectory: false);
        Console.WriteLine($"Wrote {zip} ({new FileInfo(zip).Length:N0} bytes), pack version {manifest.PackVersion}.");
        return 0;
    }
    default:
        Console.Error.WriteLine("usage: DataManifest [update|check|pack <out.zip>] [--pack-version <v>] [--data <dir>]");
        return 1;
}

static bool Check(DataManifest manifest, string data)
{
    var problems = manifest.Verify(data);
    foreach (var p in problems) Console.WriteLine(p);
    Warn(manifest, data);
    Console.WriteLine(problems.Count == 0 ? $"OK: pack version {manifest.PackVersion}, {manifest.Datasets.Sum(d => d.Files.Count)} files." : $"{problems.Count} problems.");
    return problems.Count == 0;
}

// sheva-na.json and English\*.txt are generated from MAM and Phonetic MAM, so the two must be from the same commit.
static void Warn(DataManifest manifest, string data)
{
    if (manifest["MAM"]?.Version != manifest["PhoneticMAM"]?.Version)
        Console.WriteLine("warning: MAM and PhoneticMAM have different versions; they must come from the same MAM-basics commit " +
                          "(then rerun Tools\\ShevaNaGenerator and Tools\\EnglishGenerator).");
    foreach (var dir in Directory.EnumerateDirectories(data).Select(Path.GetFileName))
        if (manifest[dir!] == null) Console.WriteLine($"warning: folder {dir} is not in the manifest");
}
