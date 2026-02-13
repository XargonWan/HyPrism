using HyPrism.Services.Core.App;
using HyPrism.Services.Core.Infrastructure;
using HyPrism.Services.Game.Instance;
using System.Text.Json;
using System.Text.Json.Serialization;
using HyPrism.Models;
using System.Text.RegularExpressions;
using System.Net.Http.Json;

namespace HyPrism.Services.Game.Mod;

/// <summary>
/// Manages game modifications including searching, installing, updating, and tracking.
/// Integrates with CurseForge API for mod discovery and downloading.
/// </summary>
public class ModService : IModService
{
    private readonly HttpClient _httpClient;
    private readonly string _appDir;
    
    // CurseForge API base URL
    private const string CfApiBaseUrl = "https://api.curseforge.com";
    
    // CF Website Base URL
    private const string CfBaseUrl = "https://www.curseforge.com";

    // Hytale game ID on CurseForge
    private const int HytaleGameId = 70216;

    // Lock for mod manifest operations to prevent concurrent writes
    private static readonly SemaphoreSlim _modManifestLock = new(1, 1);
    
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ConfigService _configService;
    private readonly InstanceService _instanceService;
    private readonly ProgressNotificationService _progressNotificationService;
    
    /// <summary>
    /// Gets the CurseForge API key from configuration.
    /// </summary>
    private string CurseForgeApiKey => _configService.Configuration.CurseForgeKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModService"/> class.
    /// </summary>
    public ModService(
        HttpClient httpClient, 
        string appDir,
        ConfigService configService,
        InstanceService instanceService,
        ProgressNotificationService progressNotificationService)
    {
        _httpClient = httpClient;
        _appDir = appDir;
        _configService = configService;
        _instanceService = instanceService;
        _progressNotificationService = progressNotificationService;
    }
    
    /// <summary>
    /// Creates an HttpRequestMessage for CurseForge API with proper headers.
    /// </summary>
    private HttpRequestMessage CreateCurseForgeRequest(HttpMethod method, string endpoint)
    {
        var request = new HttpRequestMessage(method, $"{CfApiBaseUrl}{endpoint}");
        request.Headers.Add("x-api-key", CurseForgeApiKey);
        request.Headers.Add("Accept", "application/json");
        return request;
    }
    
    /// <summary>
    /// Validates that the CurseForge API key is available.
    /// </summary>
    private bool HasApiKey()
    {
        if (!string.IsNullOrEmpty(CurseForgeApiKey)) return true;
        Logger.Warning("ModService", "CurseForge API key is not configured");
        return false;
    }
    
    /// <inheritdoc/>
    public async Task<ModSearchResult> SearchModsAsync(string query, int page, int pageSize, string[] categories, int sortField, int sortOrder)
    {
        if (!HasApiKey())
            return new ModSearchResult { Mods = new List<ModInfo>(), TotalCount = 0 };

        try
        {
            var index = page * pageSize;
            var sortOrderStr = sortOrder == 0 ? "asc" : "desc";
            var endpoint = $"/v1/mods/search?gameId={HytaleGameId}" +
                           $"&searchFilter={Uri.EscapeDataString(query)}" +
                           $"&index={index}&pageSize={pageSize}" +
                           $"&sortField={sortField}&sortOrder={sortOrderStr}";
            
            if (categories is { Length: > 0 })
            {
                var catId = categories[0];
                if (int.TryParse(catId, out var categoryId) && categoryId > 0)
                    endpoint += $"&categoryId={categoryId}";
            }
            
            using var request = CreateCurseForgeRequest(HttpMethod.Get, endpoint);
            using var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                Logger.Warning("ModService", $"CurseForge search returned {response.StatusCode}");
                return new ModSearchResult { Mods = new List<ModInfo>(), TotalCount = 0 };
            }

            var json = await response.Content.ReadAsStringAsync();
            var cfResponse = JsonSerializer.Deserialize<CurseForgeSearchResponse>(json, _jsonOptions);
            
            if (cfResponse?.Data == null)
                return new ModSearchResult { Mods = new List<ModInfo>(), TotalCount = 0 };

            var mods = cfResponse.Data.Select(MapToModInfo).ToList();
            
            return new ModSearchResult
            {
                Mods = mods,
                TotalCount = cfResponse.Pagination?.TotalCount ?? mods.Count
            };
        }
        catch (Exception ex)
        {
            Logger.Error("ModService", $"Search failed: {ex.Message}");
            return new ModSearchResult { Mods = new List<ModInfo>(), TotalCount = 0 };
        }
    }

    /// <inheritdoc/>
    public async Task<List<ModCategory>> GetModCategoriesAsync()
    {
        if (!HasApiKey())
            return GetFallbackCategories();
        
        try
        {
            var endpoint = $"/v1/categories?gameId={HytaleGameId}";
            using var request = CreateCurseForgeRequest(HttpMethod.Get, endpoint);
            using var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                Logger.Warning("ModService", $"Categories request returned {response.StatusCode}");
                return GetFallbackCategories();
            }
            
            var json = await response.Content.ReadAsStringAsync();
            var cfResponse = JsonSerializer.Deserialize<CurseForgeCategoriesResponse>(json, _jsonOptions);
            
            if (cfResponse?.Data == null || cfResponse.Data.Count == 0)
                return GetFallbackCategories();
            
            // Find the "Mods" class category dynamically (matching original repo)
            var modsClass = cfResponse.Data.FirstOrDefault(c => c.IsClass == true &&
                string.Equals(c.Name, "mods", StringComparison.OrdinalIgnoreCase));
            int modsClassId = modsClass?.Id ?? 0;
            
            var categories = new List<ModCategory>
            {
                new ModCategory { Id = 0, Name = "All Mods", Slug = "all" }
            };
            
            // Get subcategories under the Mods class
            var modCategories = cfResponse.Data
                .Where(c => c.ParentCategoryId == modsClassId && c.IsClass != true)
                .Select(c => new ModCategory
                {
                    Id = c.Id,
                    Name = c.Name ?? "",
                    Slug = c.Slug ?? ""
                })
                .OrderBy(c => c.Name)
                .ToList();
            
            // Fallback: if no subcategories found, return all non-class categories
            if (modCategories.Count == 0)
            {
                modCategories = cfResponse.Data
                    .Where(c => c.IsClass != true)
                    .Select(c => new ModCategory
                    {
                        Id = c.Id,
                        Name = c.Name ?? "",
                        Slug = c.Slug ?? ""
                    })
                    .OrderBy(c => c.Name)
                    .ToList();
            }
            
            categories.AddRange(modCategories);
            
            return categories;
        }
        catch (Exception ex)
        {
            Logger.Warning("ModService", $"Failed to load categories: {ex.Message}");
            return GetFallbackCategories();
        }
    }
    
    private static List<ModCategory> GetFallbackCategories()
    {
        return new List<ModCategory>
        {
            new ModCategory { Id = 0, Name = "All Mods", Slug = "all" },
            new ModCategory { Id = 2, Name = "World Gen", Slug = "world-gen" },
            new ModCategory { Id = 3, Name = "Magic", Slug = "magic" },
            new ModCategory { Id = 4, Name = "Tech", Slug = "tech" }
        };
    }

    /// <inheritdoc/>
    public async Task<bool> InstallModFileToInstanceAsync(string slugOrId, string fileIdOrVersion, string instancePath, Action<string, string>? onProgress = null)
    {
        if (!HasApiKey()) return false;

        try
        {
            // Get file info first
            var fileEndpoint = $"/v1/mods/{slugOrId}/files/{fileIdOrVersion}";
            using var fileRequest = CreateCurseForgeRequest(HttpMethod.Get, fileEndpoint);
            using var fileResponse = await _httpClient.SendAsync(fileRequest);
            
            if (!fileResponse.IsSuccessStatusCode)
            {
                Logger.Warning("ModService", $"Get file info returned {fileResponse.StatusCode}");
                return false;
            }
            
            var fileJson = await fileResponse.Content.ReadAsStringAsync();
            var cfFileResp = JsonSerializer.Deserialize<CurseForgeFileResponse>(fileJson, _jsonOptions);
            var cfFile = cfFileResp?.Data;
            
            if (cfFile == null || string.IsNullOrEmpty(cfFile.DownloadUrl))
            {
                Logger.Warning("ModService", "File info missing or no download URL");
                return false;
            }
            
            onProgress?.Invoke("downloading", cfFile.FileName ?? "mod file");
            
            // Download the file to UserData/Mods folder (correct Hytale mod location)
            var modsPath = Path.Combine(instancePath, "UserData", "Mods");
            Directory.CreateDirectory(modsPath);
            
            var filePath = Path.Combine(modsPath, cfFile.FileName ?? $"mod_{cfFile.Id}.jar");
            
            using var downloadResponse = await _httpClient.GetAsync(cfFile.DownloadUrl);
            if (!downloadResponse.IsSuccessStatusCode)
            {
                Logger.Warning("ModService", $"Download returned {downloadResponse.StatusCode}");
                return false;
            }
            
            await using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            await downloadResponse.Content.CopyToAsync(fs);
            
            onProgress?.Invoke("installing", cfFile.FileName ?? "mod file");
            
            // Get the actual numeric mod ID from the file response
            var numericModId = cfFile.ModId > 0 ? cfFile.ModId.ToString() : slugOrId;
            
            // Also get mod info for the manifest
            CurseForgeMod? modInfo = null;
            try
            {
                // Use numeric ID for mod info request
                var modEndpoint = $"/v1/mods/{numericModId}";
                using var modRequest = CreateCurseForgeRequest(HttpMethod.Get, modEndpoint);
                using var modResponse = await _httpClient.SendAsync(modRequest);
                if (modResponse.IsSuccessStatusCode)
                {
                    var modJson = await modResponse.Content.ReadAsStringAsync();
                    var modResp = JsonSerializer.Deserialize<CurseForgeModResponse>(modJson, _jsonOptions);
                    modInfo = modResp?.Data;
                }
            }
            catch { /* Non-critical */ }
            
            // Add to manifest
            var mods = GetInstanceInstalledMods(instancePath);
            
            // Remove existing entry for this mod if any (check both numeric ID and old slug-based ID)
            mods.RemoveAll(m => m.CurseForgeId == numericModId || m.CurseForgeId == slugOrId || m.Id == $"cf-{numericModId}" || m.Id == $"cf-{slugOrId}");
            
            var installedMod = new InstalledMod
            {
                Id = $"cf-{numericModId}",
                Name = modInfo?.Name ?? cfFile.DisplayName ?? cfFile.FileName ?? "Unknown Mod",
                Slug = modInfo?.Slug ?? "",
                Version = ExtractVersion(cfFile.DisplayName, cfFile.FileName),
                FileId = cfFile.Id.ToString(),
                FileName = cfFile.FileName ?? "",
                Enabled = true,
                Author = modInfo?.Authors?.FirstOrDefault()?.Name ?? "",
                Description = modInfo?.Summary ?? "",
                IconUrl = modInfo?.Logo?.ThumbnailUrl ?? "",
                CurseForgeId = numericModId,  // Always save numeric ID
                FileDate = cfFile.FileDate ?? "",
                ReleaseType = cfFile.ReleaseType,
                Screenshots = modInfo?.Screenshots?.Select(s => new CurseForgeScreenshot
                {
                    Id = s.Id,
                    Title = s.Title,
                    ThumbnailUrl = s.ThumbnailUrl,
                    Url = s.Url
                }).ToList() ?? new List<CurseForgeScreenshot>()
            };
            
            mods.Add(installedMod);
            await SaveInstanceModsAsync(instancePath, mods);
            
            onProgress?.Invoke("complete", cfFile.FileName ?? "mod file");
            Logger.Success("ModService", $"Installed mod {installedMod.Name} (ID: {numericModId}) to {instancePath}");
            
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error("ModService", $"Install failed: {ex.Message}");
            return false;
        }
    }

    /// <inheritdoc/>
    public List<InstalledMod> GetInstanceInstalledMods(string instancePath)
    {
        var modsPath = Path.Combine(instancePath, "Client", "mods");
        var manifestPath = Path.Combine(modsPath, "manifest.json");

        if (!File.Exists(manifestPath)) return new List<InstalledMod>(); 
        
        try
        {
            var json = File.ReadAllText(manifestPath);
            return JsonSerializer.Deserialize<List<InstalledMod>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<InstalledMod>();
        }
        catch 
        {
            return new List<InstalledMod>();
        }
    }
    
    /// <inheritdoc/>
    public async Task SaveInstanceModsAsync(string instancePath, List<InstalledMod> mods)
    {
        await _modManifestLock.WaitAsync();
        try
        {
            var modsPath = Path.Combine(instancePath, "Client", "mods");
            Directory.CreateDirectory(modsPath);
            var manifestPath = Path.Combine(modsPath, "manifest.json");
            
            var json = JsonSerializer.Serialize(mods, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(manifestPath, json);
        }
        finally
        {
            _modManifestLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<ModFilesResult> GetModFilesAsync(string modId, int page, int pageSize)
    {
        if (!HasApiKey())
            return new ModFilesResult();

        try
        {
            var index = page * pageSize;
            var endpoint = $"/v1/mods/{modId}/files?index={index}&pageSize={pageSize}";
            using var request = CreateCurseForgeRequest(HttpMethod.Get, endpoint);
            using var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                Logger.Warning("ModService", $"Get mod files returned {response.StatusCode}");
                return new ModFilesResult();
            }
            
            var json = await response.Content.ReadAsStringAsync();
            var cfResponse = JsonSerializer.Deserialize<CurseForgeFilesResponse>(json, _jsonOptions);
            
            if (cfResponse?.Data == null)
                return new ModFilesResult();
            
            return new ModFilesResult
            {
                Files = cfResponse.Data.Select(f => new ModFileInfo
                {
                    Id = f.Id.ToString(),
                    ModId = f.ModId.ToString(),
                    FileName = f.FileName ?? "",
                    DisplayName = f.DisplayName ?? "",
                    DownloadUrl = f.DownloadUrl ?? "",
                    FileLength = f.FileLength,
                    FileDate = f.FileDate ?? "",
                    ReleaseType = f.ReleaseType,
                    GameVersions = f.GameVersions ?? new List<string>(),
                    DownloadCount = f.DownloadCount
                }).ToList(),
                TotalCount = cfResponse.Pagination?.TotalCount ?? cfResponse.Data.Count
            };
        }
        catch (Exception ex)
        {
            Logger.Error("ModService", $"Get mod files failed: {ex.Message}");
            return new ModFilesResult();
        }
    }

    /// <inheritdoc/>
    public async Task<List<InstalledMod>> CheckInstanceModUpdatesAsync(string instancePath)
    {
        if (!HasApiKey())
            return new List<InstalledMod>();
            
        var installedMods = GetInstanceInstalledMods(instancePath);
        var modsWithUpdates = new List<InstalledMod>();
        
        foreach (var mod in installedMods)
        {
            if (string.IsNullOrEmpty(mod.CurseForgeId)) continue;
            
            try
            {
                var endpoint = $"/v1/mods/{mod.CurseForgeId}/files?pageSize=1";
                using var request = CreateCurseForgeRequest(HttpMethod.Get, endpoint);
                using var response = await _httpClient.SendAsync(request);
                
                if (!response.IsSuccessStatusCode) continue;
                
                var json = await response.Content.ReadAsStringAsync();
                var cfResponse = JsonSerializer.Deserialize<CurseForgeFilesResponse>(json, _jsonOptions);
                
                var latestFile = cfResponse?.Data?.FirstOrDefault();
                if (latestFile == null) continue;
                
                // If we have a newer file than what's installed
                if (!string.IsNullOrEmpty(mod.FileId) && latestFile.Id.ToString() != mod.FileId)
                {
                    mod.LatestFileId = latestFile.Id.ToString();
                    mod.LatestVersion = latestFile.DisplayName ?? "";
                    modsWithUpdates.Add(mod);
                }
            }
            catch (Exception ex)
            {
                Logger.Warning("ModService", $"Update check failed for {mod.Name}: {ex.Message}");
            }
        }
        
        return modsWithUpdates;
    }

    /// <inheritdoc/>
    public async Task<bool> InstallLocalModFile(string sourcePath, string instancePath)
    {
        try
        {
            if (!File.Exists(sourcePath))
            {
                Logger.Warning("ModService", $"Source mod file not found: {sourcePath}");
                return false;
            }
            
            var modsPath = Path.Combine(instancePath, "Client", "mods");
            Directory.CreateDirectory(modsPath);
            
            var fileName = Path.GetFileName(sourcePath);
            var destPath = Path.Combine(modsPath, fileName);
            
            File.Copy(sourcePath, destPath, true);
            
            // Add to manifest
            var mods = GetInstanceInstalledMods(instancePath);
            
            // Remove existing entry with same filename
            mods.RemoveAll(m => m.FileName == fileName);
            
            mods.Add(new InstalledMod
            {
                Id = $"local-{Guid.NewGuid():N}",
                Name = Path.GetFileNameWithoutExtension(fileName),
                FileName = fileName,
                Enabled = true,
                Version = "local",
                Author = "Local file"
            });
            
            await SaveInstanceModsAsync(instancePath, mods);
            Logger.Success("ModService", $"Installed local mod: {fileName}");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error("ModService", $"Install local mod failed: {ex.Message}");
            return false;
        }
    }
    
    /// <inheritdoc/>
    public async Task<bool> InstallModFromBase64(string fileName, string base64Content, string instancePath)
    {
        try
        {
            var modsPath = Path.Combine(instancePath, "Client", "mods");
            Directory.CreateDirectory(modsPath);
            
            var destPath = Path.Combine(modsPath, fileName);
            var bytes = Convert.FromBase64String(base64Content);
            await File.WriteAllBytesAsync(destPath, bytes);
            
            // Add to manifest
            var mods = GetInstanceInstalledMods(instancePath);
            mods.RemoveAll(m => m.FileName == fileName);
            
            mods.Add(new InstalledMod
            {
                Id = $"local-{Guid.NewGuid():N}",
                Name = Path.GetFileNameWithoutExtension(fileName),
                FileName = fileName,
                Enabled = true,
                Version = "local",
                Author = "Imported file"
            });
            
            await SaveInstanceModsAsync(instancePath, mods);
            Logger.Success("ModService", $"Installed mod from base64: {fileName}");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error("ModService", $"Install mod from base64 failed: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// <summary>
    /// Extracts a clean version string from CurseForge DisplayName or FileName.
    /// Looks for semver-like patterns (e.g., "1.2.7", "0.3.1-beta") and returns the first match.
    /// Falls back to DisplayName or FileName if no version pattern is found.
    /// </summary>
    private static string ExtractVersion(string? displayName, string? fileName)
    {
        // Try to extract a semver-like version from displayName first, then fileName
        var versionRegex = new Regex(@"(\d+\.\d+(?:\.\d+)?(?:[-.]\w+)*)");
        
        if (!string.IsNullOrEmpty(displayName))
        {
            var match = versionRegex.Match(displayName);
            if (match.Success) return match.Groups[1].Value;
        }
        
        if (!string.IsNullOrEmpty(fileName))
        {
            // Strip extension first
            var name = fileName;
            if (name.EndsWith(".jar", StringComparison.OrdinalIgnoreCase))
                name = name[..^4];
            else if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                name = name[..^4];
                
            var match = versionRegex.Match(name);
            if (match.Success) return match.Groups[1].Value;
        }
        
        return displayName ?? fileName ?? "";
    }

    /// <summary>
    /// Maps a CurseForge API mod to the normalized ModInfo.
    /// </summary>
    private static ModInfo MapToModInfo(CurseForgeMod cfMod)
    {
        return new ModInfo
        {
            Id = cfMod.Id.ToString(),
            Name = cfMod.Name ?? "",
            Slug = cfMod.Slug ?? "",
            Summary = cfMod.Summary ?? "",
            Author = cfMod.Authors?.FirstOrDefault()?.Name ?? "",
            DownloadCount = cfMod.DownloadCount,
            IconUrl = cfMod.Logo?.ThumbnailUrl ?? "",
            ThumbnailUrl = cfMod.Logo?.Url ?? "",
            Categories = cfMod.Categories?.Select(c => c.Name ?? "").Where(n => !string.IsNullOrEmpty(n)).ToList() ?? new List<string>(),
            DateUpdated = cfMod.DateModified ?? "",
            LatestFileId = cfMod.LatestFiles?.FirstOrDefault()?.Id.ToString() ?? "",
            Screenshots = cfMod.Screenshots ?? new List<CurseForgeScreenshot>()
        };
    }
}
