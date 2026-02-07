using HyPrism.Models;

namespace HyPrism.Services.Game;

/// <summary>
/// Manages game instances including installation paths, version tracking, and instance lifecycle.
/// </summary>
public interface IInstanceService
{
    /// <summary>
    /// Gets the root directory where all game instances are stored.
    /// </summary>
    /// <returns>The absolute path to the instances root directory.</returns>
    string GetInstanceRoot();

    /// <summary>
    /// Gets the directory path for a specific game branch.
    /// </summary>
    /// <param name="branch">The game branch ("release" or "pre-release").</param>
    /// <returns>The absolute path to the branch directory.</returns>
    string GetBranchPath(string branch);

    /// <summary>
    /// Gets the user data path for a specific game instance.
    /// </summary>
    /// <param name="versionPath">The path to the game version directory.</param>
    /// <returns>The absolute path to the user data directory.</returns>
    string GetInstanceUserDataPath(string versionPath);

    /// <summary>
    /// Resolves a version number, returning the latest if the specified version is not available.
    /// </summary>
    /// <param name="branch">The game branch.</param>
    /// <param name="version">The requested version number.</param>
    /// <returns>The resolved version number.</returns>
    int ResolveVersionOrLatest(string branch, int version);

    /// <summary>
    /// Finds an existing instance path for the specified branch and version.
    /// </summary>
    /// <param name="branch">The game branch.</param>
    /// <param name="version">The version number.</param>
    /// <returns>The path to the existing instance, or <c>null</c> if not found.</returns>
    string? FindExistingInstancePath(string branch, int version);

    /// <summary>
    /// Gets all instance root paths including legacy installation locations.
    /// </summary>
    /// <returns>An enumerable of all instance root paths.</returns>
    IEnumerable<string> GetInstanceRootsIncludingLegacy();

    /// <summary>
    /// Gets the path to the latest installed instance for a branch.
    /// </summary>
    /// <param name="branch">The game branch.</param>
    /// <returns>The path to the latest instance directory.</returns>
    string GetLatestInstancePath(string branch);

    /// <summary>
    /// Gets the path to the latest instance info file for a branch.
    /// </summary>
    /// <param name="branch">The game branch.</param>
    /// <returns>The path to the latest info JSON file.</returns>
    string GetLatestInfoPath(string branch);

    /// <summary>
    /// Loads the latest instance info from disk.
    /// </summary>
    /// <param name="branch">The game branch.</param>
    /// <returns>The latest instance info, or <c>null</c> if not found.</returns>
    LatestInstanceInfo? LoadLatestInfo(string branch);

    /// <summary>
    /// Saves the latest instance info to disk.
    /// </summary>
    /// <param name="branch">The game branch.</param>
    /// <param name="version">The version number to save as latest.</param>
    void SaveLatestInfo(string branch, int version);

    /// <summary>
    /// Migrates data from legacy installation formats to the current structure.
    /// </summary>
    void MigrateLegacyData();

    /// <summary>
    /// Checks if the game client executable is present at the specified path.
    /// </summary>
    /// <param name="versionPath">The path to the game version directory.</param>
    /// <returns><c>true</c> if the client is present; otherwise, <c>false</c>.</returns>
    bool IsClientPresent(string versionPath);

    /// <summary>
    /// Checks if the game assets are present at the specified path.
    /// </summary>
    /// <param name="versionPath">The path to the game version directory.</param>
    /// <returns><c>true</c> if assets are present; otherwise, <c>false</c>.</returns>
    bool AreAssetsPresent(string versionPath);

    /// <summary>
    /// Gets the path for a specific game instance.
    /// </summary>
    /// <param name="branch">The game branch.</param>
    /// <param name="version">The version number.</param>
    /// <returns>The absolute path to the instance directory.</returns>
    string GetInstancePath(string branch, int version);

    /// <summary>
    /// Resolves the instance path, optionally preferring existing installations.
    /// </summary>
    /// <param name="branch">The game branch.</param>
    /// <param name="version">The version number.</param>
    /// <param name="preferExisting">Whether to prefer existing installations over creating new paths.</param>
    /// <returns>The resolved instance path.</returns>
    string ResolveInstancePath(string branch, int version, bool preferExisting);

    /// <summary>
    /// Deletes a game instance from disk.
    /// </summary>
    /// <param name="branch">The game branch.</param>
    /// <param name="versionNumber">The version number to delete.</param>
    /// <returns><c>true</c> if the instance was successfully deleted; otherwise, <c>false</c>.</returns>
    bool DeleteGame(string branch, int versionNumber);

    /// <summary>
    /// Gets a list of all installed game instances.
    /// </summary>
    /// <returns>A list of installed instance metadata.</returns>
    List<InstalledInstance> GetInstalledInstances();
}
