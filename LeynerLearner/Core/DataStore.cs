using System.IO.Compression;

namespace LeynerLearner.Core;

/// <summary>
/// Where the app's data comes from: the Data folder next to the program (bundled with it), or an updated data pack
/// installed in %LOCALAPPDATA%\LeynerLearner\Data. The installed pack is used only while its manifest reads, every
/// file matches its hash and it is not older than the bundled data.
/// </summary>
public static class DataStore
{
    /// <summary>Where "Update data" downloads the current data pack (a zip made by Tools\DataManifest pack).
    /// The LEYNERLEARNER_DATA_URL environment variable overrides it, for testing a pack before publishing it.
    /// (A private repository's releases need a sign-in, so this works only once the repository is public.)</summary>
    public const string DefaultPackUrl = "https://github.com/dwalls47/LeynerLearner/releases/latest/download/LeynerLearner-data.zip";

    public static string PackUrl => Environment.GetEnvironmentVariable("LEYNERLEARNER_DATA_URL") is { Length: > 0 } url ? url : DefaultPackUrl;

    public static string BundledDir => Path.Combine(AppContext.BaseDirectory, "Data");

    static string AppDataDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LeynerLearner");

    public static string InstalledDir => Path.Combine(AppDataDir, "Data");

    static string StagingRoot => Path.Combine(AppDataDir, "Staging");

    /// <summary>The data folder to read, with its manifest (null if the bundled data has none), and why the installed pack is not used, if it is not.</summary>
    /// <remarks><paramref name="Outdated"/>: the installed pack is fine but older than the bundled data (the app was updated since).</remarks>
    public sealed record Choice(string Dir, DataManifest? Manifest, bool IsInstalled, string? InstalledProblem, bool Outdated = false);

    public static Choice Choose()
    {
        var bundled = DataManifest.TryLoad(BundledDir);
        if (!Directory.Exists(InstalledDir)) return new(BundledDir, bundled, false, null);

        string? problem;
        var installed = DataManifest.TryLoad(InstalledDir);
        if (installed == null) problem = "its manifest is missing or cannot be read";
        else if (bundled != null && string.CompareOrdinal(installed.PackVersion, bundled.PackVersion) < 0)
            return new(BundledDir, bundled, false,
                       $"it (version {installed.PackVersion}) is older than the built-in data (version {bundled.PackVersion})", Outdated: true);
        else
        {
            List<string> problems;
            try { problems = installed.Verify(InstalledDir); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { problems = [e.Message]; }
            if (problems.Count == 0) return new(InstalledDir, installed, true, null);
            problem = problems.Count == 1 ? problems[0].TrimEnd('.') : $"{problems[0].TrimEnd('.')} (and {problems.Count - 1} more problems)";
        }
        return new(BundledDir, bundled, false, problem);
    }

    /// <summary>A downloaded or chosen pack, extracted and verified, waiting to be installed.</summary>
    public sealed record StagedPack(string Dir, DataManifest Manifest);

    /// <summary>Downloads the pack at <see cref="PackUrl"/> and stages it.</summary>
    public static async Task<StagedPack> DownloadAsync(IProgress<string> progress, CancellationToken cancel)
    {
        Directory.CreateDirectory(StagingRoot);
        string zip = Path.Combine(StagingRoot, "download.zip");
        try
        {
            using (var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) })
            {
                http.DefaultRequestHeaders.UserAgent.ParseAdd("LeynerLearner");
                using var response = await http.GetAsync(PackUrl, HttpCompletionOption.ResponseHeadersRead, cancel);
                response.EnsureSuccessStatusCode();
                long? total = response.Content.Headers.ContentLength;
                await using var source = await response.Content.ReadAsStreamAsync(cancel);
                await using var target = File.Create(zip);
                var buffer = new byte[81920];
                long done = 0;
                int n;
                while ((n = await source.ReadAsync(buffer, cancel)) > 0)
                {
                    await target.WriteAsync(buffer.AsMemory(0, n), cancel);
                    done += n;
                    progress.Report(total > 0 ? $"Downloading… {done * 100 / total}% of {total / 1048576.0:0.0} MB" : $"Downloading… {done / 1048576.0:0.0} MB");
                }
            }
            return await StageAsync(zip, progress, cancel);
        }
        finally
        {
            TryDelete(zip);
        }
    }

    /// <summary>Extracts a pack zip to a new staging folder and checks every file against the pack's own manifest.</summary>
    public static Task<StagedPack> StageAsync(string zip, IProgress<string> progress, CancellationToken cancel) => Task.Run(() =>
    {
        string dir = Path.Combine(StagingRoot, Path.GetRandomFileName());
        try
        {
            progress.Report("Unpacking…");
            // ExtractToDirectory refuses entries that would land outside the folder.
            ZipFile.ExtractToDirectory(zip, dir);
            cancel.ThrowIfCancellationRequested();
            // A pack may have its Data folder at the top of the zip or inside one folder.
            string root = dir;
            if (!File.Exists(Path.Combine(root, DataManifest.FileName)) && Directory.GetFiles(root).Length == 0 &&
                Directory.GetDirectories(root) is [var only])
                root = only;
            var manifest = DataManifest.TryLoad(root) ?? throw new InvalidDataException("The pack has no readable manifest.json.");
            progress.Report("Checking files…");
            var problems = manifest.Verify(root, cancel);
            if (problems.Count > 0)
                throw new InvalidDataException("The pack failed its check:\n" + string.Join("\n", problems.Take(10)) +
                                               (problems.Count > 10 ? $"\n(and {problems.Count - 10} more)" : ""));
            return new StagedPack(root == dir ? dir : MoveUp(root, dir), manifest);
        }
        catch
        {
            TryDeleteDir(dir);
            throw;
        }
    }, cancel);

    // Makes the pack's Data folder the staging folder itself, so installing moves just it.
    static string MoveUp(string root, string dir)
    {
        string flat = dir + ".pack";
        Directory.Move(root, flat);
        TryDeleteDir(dir);
        return flat;
    }

    /// <summary>
    /// Makes a staged pack the installed data, replacing any installed before. The caller must have released any
    /// file it holds open in the installed folder (the font).
    /// </summary>
    public static void Install(StagedPack pack)
    {
        Directory.CreateDirectory(AppDataDir);
        string old = InstalledDir + ".old";
        TryDeleteDir(old);
        if (Directory.Exists(InstalledDir)) Directory.Move(InstalledDir, old);
        try
        {
            Directory.Move(pack.Dir, InstalledDir);
        }
        catch
        {
            if (Directory.Exists(old) && !Directory.Exists(InstalledDir)) Directory.Move(old, InstalledDir);
            throw;
        }
        TryDeleteDir(old);
        TryDeleteDir(StagingRoot);
    }

    /// <summary>Removes the installed pack, so the bundled data is used. The caller must have released its font.</summary>
    public static void RemoveInstalled()
    {
        if (Directory.Exists(InstalledDir)) Directory.Delete(InstalledDir, recursive: true);
        TryDeleteDir(InstalledDir + ".old");
    }

    public static void Discard(StagedPack pack) => TryDeleteDir(pack.Dir);

    /// <summary>
    /// A copy of a font file outside the data folders, named by its hash. Windows keeps a loaded font file open
    /// (as long as any window uses the font), which would stop the data folder from being replaced or removed.
    /// Copies no longer in use are deleted.
    /// </summary>
    public static string FontCopy(string font)
    {
        string dir = Path.Combine(AppDataDir, "Fonts");
        Directory.CreateDirectory(dir);
        string copy = Path.Combine(dir, DataManifest.Hash(font) + Path.GetExtension(font));
        if (!File.Exists(copy)) File.Copy(font, copy);
        foreach (var other in Directory.EnumerateFiles(dir).Where(f => !f.Equals(copy, StringComparison.OrdinalIgnoreCase)))
            TryDelete(other);    // fails, harmlessly, for a copy another running copy of the app has loaded
        return copy;
    }

    /// <summary>What a pack changes, dataset by dataset: "MAM: 202e69e → 3f1c2aa (12 files changed)".</summary>
    public static List<string> Changes(DataManifest? from, DataManifest to)
    {
        var lines = new List<string>();
        foreach (var d in to.Datasets)
        {
            var was = from?[d.Folder];
            var oldFiles = was?.Files.ToDictionary(f => f.Path, f => f.Sha256, StringComparer.Ordinal) ?? [];
            int changed = d.Files.Count(f => !oldFiles.TryGetValue(f.Path, out var h) || !h.Equals(f.Sha256, StringComparison.OrdinalIgnoreCase));
            int removed = oldFiles.Keys.Count(p => d.Files.All(f => f.Path != p));
            string files = changed + removed == 0 ? "no changes" :
                string.Join(", ", new[] { changed > 0 ? $"{changed} file{(changed == 1 ? "" : "s")} new or changed" : null,
                                          removed > 0 ? $"{removed} removed" : null }.OfType<string>());
            string version = was == null ? $"(new) → {Short(d.Version)}" :
                             was.Version == d.Version ? Short(d.Version) : $"{Short(was.Version)} → {Short(d.Version)}";
            lines.Add($"{d.Name}: {version} ({files})");
        }
        foreach (var gone in from?.Datasets.Where(d => to[d.Folder] == null) ?? [])
            lines.Add($"{gone.Name}: removed");
        return lines;
    }

    // A 40-hex-digit commit is shown as its first 7 digits.
    static string Short(string version) =>
        version.Length == 40 && version.All(Uri.IsHexDigit) ? version[..7] : version;

    static void TryDelete(string path)
    {
        try { File.Delete(path); } catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
    }

    static void TryDeleteDir(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, recursive: true); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
    }
}
