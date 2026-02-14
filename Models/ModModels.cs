using System.Collections.Generic;

namespace HyPrism.Models;

public class ModSearchResult
{
    public List<ModInfo> Mods { get; set; } = new();
    public int TotalCount { get; set; }
}

public class ModInfo
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Description { get; set; } = "";
    public string Author { get; set; } = "";
    public int DownloadCount { get; set; }
    public string IconUrl { get; set; } = "";
    public string ThumbnailUrl { get; set; } = "";
    public List<string> Categories { get; set; } = new();
    public string DateUpdated { get; set; } = "";
    public string LatestFileId { get; set; } = "";
    public List<CurseForgeScreenshot> Screenshots { get; set; } = new();
}

public class ModFilesResult
{
    public List<ModFileInfo> Files { get; set; } = new();
    public int TotalCount { get; set; }
}

public class ModFileInfo
{
    public string Id { get; set; } = "";
    public string ModId { get; set; } = "";
    public string FileName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
    public long FileLength { get; set; }
    public string FileDate { get; set; } = "";
    public int ReleaseType { get; set; }
    public List<string> GameVersions { get; set; } = new();
    public int DownloadCount { get; set; }
}

public class ModCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
}

public class InstalledMod
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Version { get; set; } = "";
    public string FileId { get; set; } = "";
    public string FileName { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public string Author { get; set; } = "";
    public string Description { get; set; } = "";
    public string IconUrl { get; set; } = "";
    public string CurseForgeId { get; set; } = "";
    public string FileDate { get; set; } = "";
    
    /// <summary>
    /// CurseForge release type: 1 = Release, 2 = Beta, 3 = Alpha
    /// </summary>
    public int ReleaseType { get; set; } = 1;
    
    public List<CurseForgeScreenshot> Screenshots { get; set; } = new();
    
    /// <summary>
    /// The latest available file ID from CurseForge (for update checking).
    /// </summary>
    public string LatestFileId { get; set; } = "";
    
    /// <summary>
    /// The latest available version string from CurseForge (for update display).
    /// </summary>
    public string LatestVersion { get; set; } = "";

    /// <summary>
    /// Original file extension used before disabling (e.g. .jar or .zip).
    /// </summary>
    public string DisabledOriginalExtension { get; set; } = "";
}

/// <summary>
/// Entry for mod list import/export
/// </summary>
public class ModListEntry
{
    public string? CurseForgeId { get; set; }
    public string? FileId { get; set; }
    public string? Name { get; set; }
    public string? Version { get; set; }
}

public class ModUpdate
{
    public string ModId { get; set; } = "";
    public string CurrentFileId { get; set; } = "";
    public string LatestFileId { get; set; } = "";
    public string LatestFileName { get; set; } = "";
}
