using HyPrism.Models;

namespace HyPrism.Services.Game;

/// <summary>
/// Provides version management and update detection for game installations.
/// </summary>
public interface IVersionService
{
    /// <summary>
    /// Gets the list of available versions for a branch from the server.
    /// </summary>
    /// <param name="branch">The game branch.</param>
    /// <param name="ct">Token to cancel the request.</param>
    /// <returns>A list of available version numbers, sorted descending.</returns>
    Task<List<int>> GetVersionListAsync(string branch, CancellationToken ct = default);

    /// <summary>
    /// Attempts to retrieve cached version information if it's still valid.
    /// </summary>
    /// <param name="branch">The game branch.</param>
    /// <param name="maxAge">The maximum age of cached data to accept.</param>
    /// <param name="versions">The cached versions if found and valid.</param>
    /// <returns><c>true</c> if valid cached data was found; otherwise, <c>false</c>.</returns>
    bool TryGetCachedVersions(string branch, TimeSpan maxAge, out List<int> versions);

    /// <summary>
    /// Checks if the latest installed version needs an update.
    /// </summary>
    /// <param name="branch">The game branch.</param>
    /// <param name="isClientPresent">Function to check if client exists at a path.</param>
    /// <param name="getLatestInstancePath">Function to get the latest instance path.</param>
    /// <param name="loadLatestInfo">Function to load the latest version info from a path.</param>
    /// <returns><c>true</c> if an update is available; otherwise, <c>false</c>.</returns>
    Task<bool> CheckLatestNeedsUpdateAsync(string branch, Func<string, bool> isClientPresent, Func<string> getLatestInstancePath, Func<string, LatestVersionInfo?> loadLatestInfo);

    /// <summary>
    /// Gets detailed status information about the latest version.
    /// </summary>
    /// <param name="branch">The game branch.</param>
    /// <param name="isClientPresent">Function to check if client exists at a path.</param>
    /// <param name="getLatestInstancePath">Function to get the latest instance path.</param>
    /// <param name="loadLatestInfo">Function to load the latest version info from a path.</param>
    /// <returns>The version status including installed and available versions.</returns>
    Task<VersionStatus> GetLatestVersionStatusAsync(string branch, Func<string, bool> isClientPresent, Func<string> getLatestInstancePath, Func<string, LatestVersionInfo?> loadLatestInfo);

    /// <summary>
    /// Gets information about a pending update if one is available.
    /// </summary>
    /// <param name="branch">The game branch.</param>
    /// <param name="getLatestInstancePath">Function to get the latest instance path.</param>
    /// <param name="loadLatestInfo">Function to load the latest version info from a path.</param>
    /// <returns>Update information if an update is pending, or <c>null</c> if up to date.</returns>
    Task<UpdateInfo?> GetPendingUpdateInfoAsync(string branch, Func<string> getLatestInstancePath, Func<string, LatestVersionInfo?> loadLatestInfo);

    /// <summary>
    /// Calculates the sequence of patch versions needed to update from one version to another.
    /// </summary>
    /// <param name="fromVersion">The starting version number.</param>
    /// <param name="toVersion">The target version number.</param>
    /// <returns>A list of version numbers representing the patch sequence.</returns>
    List<int> GetPatchSequence(int fromVersion, int toVersion);
}
