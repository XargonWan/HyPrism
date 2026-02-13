using System.Text.Json;
using System.Text.Json.Serialization;
using HyPrism.Models;
using HyPrism.Services.Core.Infrastructure;

namespace HyPrism.Services.Game.Sources;

/// <summary>
/// Version source for community mirrors.
/// Provides fallback URLs when official Hytale servers are unavailable.
/// </summary>
/// <remarks>
/// The mirror stores two distinct types of files:
/// <list type="bullet">
///   <item><b>Release</b>: Full standalone game copies — <c>v{N}-{os}-{arch}.pwr</c>.
///     Any single file can be unpacked with Butler to get the complete game at that version.</item>
///   <item><b>Pre-release</b>: Sequential diff patches — <c>v{from}~{to}-{os}-{arch}.pwr</c>.
///     Must be applied in order to build up to the target version.</item>
/// </list>
/// </remarks>
public class MirrorVersionSource : IVersionSource
{
    private const string MirrorApiUrl = "https://thecute.cloud/ShipOfYarn/api.php";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(30);

    private readonly HttpClient _httpClient;
    private readonly string _mirrorId;
    private readonly SemaphoreSlim _fetchLock = new(1, 1);

    private MirrorIndex? _cachedIndex;
    private DateTime _cachedAt = DateTime.MinValue;

    /// <summary>
    /// Fallback URL maps loaded from versions.json cache.
    /// Key: branch → (filename → URL).
    /// </summary>
    private readonly Dictionary<string, Dictionary<string, string>> _cachedUrlsByBranch = new(StringComparer.OrdinalIgnoreCase);

    public MirrorVersionSource(HttpClient httpClient, string mirrorId = "default")
    {
        _httpClient = httpClient;
        _mirrorId = mirrorId;
    }

    #region IVersionSource Implementation

    /// <inheritdoc/>
    public string SourceId => $"mirror-{_mirrorId}";

    /// <inheritdoc/>
    public VersionSourceType Type => VersionSourceType.Mirror;

    /// <inheritdoc/>
    public bool IsAvailable => true; // Mirror is always available to try - will fetch index on demand

    /// <inheritdoc/>
    public int Priority => 100; // Lower priority than official

    /// <inheritdoc/>
    public bool IsDiffBasedBranch(string branch) =>
        branch.Equals("pre-release", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public async Task<List<CachedVersionEntry>> GetVersionsAsync(
        string os, string arch, string branch, CancellationToken ct = default)
    {
        var files = await GetPlatformFilesAsync(os, branch, ct);
        if (files == null) return new List<CachedVersionEntry>();

        var entries = new List<CachedVersionEntry>();
        string suffix = $"-{os}-{arch}.pwr";

        foreach (var (key, val) in files)
        {
            if (val.ValueKind != JsonValueKind.String) continue;
            if (!key.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) continue;
            if (!key.StartsWith("v", StringComparison.OrdinalIgnoreCase)) continue;

            string url = val.GetString()!;
            string numPart = key[1..^suffix.Length];

            int tildeIdx = numPart.IndexOf('~');
            if (tildeIdx >= 0)
            {
                // Pre-release diff format: v{from}~{to}
                string fromPart = numPart[..tildeIdx];
                string toPart = numPart[(tildeIdx + 1)..];
                if (int.TryParse(fromPart, out int fromVer) && int.TryParse(toPart, out int toVer))
                {
                    entries.Add(new CachedVersionEntry
                    {
                        Version = toVer,
                        FromVersion = fromVer,
                        PwrUrl = url
                    });
                }
            }
            else
            {
                // Release format: v{N}
                if (int.TryParse(numPart, out int ver))
                {
                    entries.Add(new CachedVersionEntry
                    {
                        Version = ver,
                        FromVersion = 0,
                        PwrUrl = url
                    });
                }
            }
        }

        // For diff-based branches (pre-release), return ALL entries - they form a patch chain
        // For release branch, group by version and prefer full downloads (FromVersion=0)
        if (IsDiffBasedBranch(branch))
        {
            // Return all patches sorted by target version
            return entries.OrderByDescending(e => e.Version).ToList();
        }
        else
        {
            // Group by version and return unique versions (prefer full downloads over diffs)
            return entries
                .GroupBy(e => e.Version)
                .Select(g => g.OrderBy(e => e.FromVersion).First()) // Prefer FromVersion=0 (full download)
                .OrderByDescending(e => e.Version)
                .ToList();
        }
    }

    /// <inheritdoc/>
    public async Task<string?> GetDownloadUrlAsync(
        string os, string arch, string branch, int version, CancellationToken ct = default)
    {
        var files = await GetPlatformFilesAsync(os, branch, ct);
        
        if (IsDiffBasedBranch(branch))
        {
            // For diff-based branches (pre-release), the "full" download is v0~1
            // which is the base installation that all patches build upon
            if (version == 1)
            {
                string diffKey = $"v0~1-{os}-{arch}.pwr";
                if (files != null && files.TryGetValue(diffKey, out var element) && element.ValueKind == JsonValueKind.String)
                    return element.GetString();
                if (_cachedUrlsByBranch.TryGetValue(branch, out var cached) && cached.TryGetValue(diffKey, out var url))
                    return url;
            }
            // For other versions in pre-release, there's no direct download - must use patch chain
            return null;
        }
        else
        {
            // Release branch: full standalone downloads
            string fileKey = $"v{version}-{os}-{arch}.pwr";
            if (files != null && files.TryGetValue(fileKey, out var element) && element.ValueKind == JsonValueKind.String)
                return element.GetString();
            if (_cachedUrlsByBranch.TryGetValue(branch, out var cached) && cached.TryGetValue(fileKey, out var url))
                return url;
        }

        return null;
    }

    /// <inheritdoc/>
    public async Task<string?> GetDiffUrlAsync(
        string os, string arch, string branch, int fromVersion, int toVersion, CancellationToken ct = default)
    {
        string fileKey = $"v{fromVersion}~{toVersion}-{os}-{arch}.pwr";

        // Try live mirror index first
        var files = await GetPlatformFilesAsync(os, branch, ct);
        if (files != null && files.TryGetValue(fileKey, out var element) && element.ValueKind == JsonValueKind.String)
            return element.GetString();

        // Fall back to cached URLs
        if (_cachedUrlsByBranch.TryGetValue(branch, out var cached) && cached.TryGetValue(fileKey, out var url))
            return url;

        return null;
    }

    /// <inheritdoc/>
    public async Task PreloadAsync(CancellationToken ct = default)
    {
        await FetchIndexAsync(ct);
    }

    #endregion

    #region Public API (for legacy compatibility)

    /// <summary>
    /// Injects cached mirror URLs so lookups work without fetching.
    /// </summary>
    public void SetCachedUrls(string branch, Dictionary<string, string> urls)
    {
        if (urls.Count > 0)
        {
            _cachedUrlsByBranch[branch] = urls;
            Logger.Info("MirrorSource", $"Loaded {urls.Count} cached URLs for branch '{branch}'");
        }
    }

    /// <summary>
    /// Returns all file URLs for a platform+branch as a flat dictionary.
    /// </summary>
    public async Task<Dictionary<string, string>> GetAllPlatformUrlsAsync(
        string os, string arch, string branch, CancellationToken ct = default)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var files = await GetPlatformFilesAsync(os, branch, ct);
        if (files == null) return result;

        string suffix = $"-{os}-{arch}.pwr";
        foreach (var (key, val) in files)
        {
            if (val.ValueKind == JsonValueKind.String && key.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                result[key] = val.GetString()!;
        }

        return result;
    }

    #endregion

    #region Internal Methods

    private async Task<Dictionary<string, JsonElement>?> GetPlatformFilesAsync(
        string os, string branch, CancellationToken ct)
    {
        var index = await GetOrFetchIndexAsync(ct);
        if (index?.Hytale == null) return null;

        string platformKey = os == "darwin" ? "mac" : os;
        if (!index.Hytale.TryGetValue(branch, out var platforms)) return null;
        if (!platforms.TryGetValue(platformKey, out var files)) return null;
        return files;
    }

    private async Task<MirrorIndex?> GetOrFetchIndexAsync(CancellationToken ct)
    {
        if (_cachedIndex != null && DateTime.UtcNow - _cachedAt < CacheTtl)
            return _cachedIndex;

        return await FetchIndexAsync(ct);
    }

    private async Task<MirrorIndex?> FetchIndexAsync(CancellationToken ct)
    {
        await _fetchLock.WaitAsync(ct);
        try
        {
            // Double-check after acquiring lock
            if (_cachedIndex != null && DateTime.UtcNow - _cachedAt < CacheTtl)
                return _cachedIndex;

            Logger.Info("MirrorSource", $"Fetching mirror index from {MirrorApiUrl}...");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(15));

            var response = await _httpClient.GetAsync(MirrorApiUrl, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                Logger.Warning("MirrorSource", $"Mirror API returned {response.StatusCode}");
                return _cachedIndex;
            }

            var json = await response.Content.ReadAsStringAsync(cts.Token);
            var index = JsonSerializer.Deserialize<MirrorIndex>(json);

            if (index?.Hytale != null)
            {
                _cachedIndex = index;
                _cachedAt = DateTime.UtcNow;

                int totalFiles = CountFiles(index);
                Logger.Success("MirrorSource", $"Mirror index loaded: {totalFiles} files across {index.Hytale.Count} branches");
            }
            else
            {
                Logger.Warning("MirrorSource", "Mirror API returned invalid/empty data");
            }

            return _cachedIndex;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            Logger.Warning("MirrorSource", "Mirror API request timed out");
            return _cachedIndex;
        }
        catch (Exception ex)
        {
            Logger.Warning("MirrorSource", $"Failed to fetch mirror index: {ex.Message}");
            return _cachedIndex;
        }
        finally
        {
            _fetchLock.Release();
        }
    }

    private static int CountFiles(MirrorIndex index)
    {
        int count = 0;
        if (index.Hytale == null) return 0;
        foreach (var branch in index.Hytale.Values)
            foreach (var platform in branch.Values)
                count += platform.Count;
        return count;
    }

    #endregion
}

/// <summary>
/// Root of the mirror API response.
/// </summary>
internal class MirrorIndex
{
    /// <summary>
    /// Map of branch -> platform -> filename -> value.
    /// </summary>
    [JsonPropertyName("hytale")]
    public Dictionary<string, Dictionary<string, Dictionary<string, JsonElement>>>? Hytale { get; set; }
}
