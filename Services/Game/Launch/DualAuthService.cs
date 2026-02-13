using System.Diagnostics;
using HyPrism.Services.Core.Infrastructure;

namespace HyPrism.Services.Game.Launch;

/// <summary>
/// Manages the DualAuth agent for server authentication.
/// DualAuth allows runtime bytecode transformation instead of static JAR patching,
/// enabling seamless dual-authentication (Official + F2P) without modifying server files.
/// </summary>
public static class DualAuthService
{
    private const string AgentUrl = "https://github.com/sanasol/hytale-auth-server/releases/latest/download/dualauth-agent.jar";
    private const string AgentFilename = "dualauth-agent.jar";
    private const int MinAgentSizeBytes = 1024;
    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(60) };

    /// <summary>
    /// Gets the path where the DualAuth agent should be stored for an instance.
    /// </summary>
    public static string GetAgentPath(string gameDir)
    {
        return Path.Combine(gameDir, "Server", AgentFilename);
    }

    /// <summary>
    /// Checks if the DualAuth agent exists and appears valid.
    /// </summary>
    public static bool IsAgentAvailable(string gameDir)
    {
        var agentPath = GetAgentPath(gameDir);
        if (!File.Exists(agentPath)) return false;

        try
        {
            var info = new FileInfo(agentPath);
            return info.Length >= MinAgentSizeBytes;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Downloads the DualAuth agent JAR if not already present or invalid.
    /// </summary>
    public static async Task<DualAuthResult> EnsureAgentAvailableAsync(
        string gameDir,
        Action<string, int?>? progressCallback = null,
        CancellationToken ct = default)
    {
        var agentPath = GetAgentPath(gameDir);
        var serverDir = Path.GetDirectoryName(agentPath)!;

        // Check if already exists and valid
        if (IsAgentAvailable(gameDir))
        {
            Logger.Info("DualAuth", "Agent already available");
            return new DualAuthResult { Success = true, AgentPath = agentPath, AlreadyExists = true };
        }

        // Ensure Server directory exists
        if (!Directory.Exists(serverDir))
        {
            Directory.CreateDirectory(serverDir);
        }

        progressCallback?.Invoke("Downloading DualAuth Agent...", 0);
        Logger.Info("DualAuth", $"Downloading agent from {AgentUrl}");

        var tempPath = agentPath + ".tmp";

        try
        {
            using var response = await _httpClient.GetAsync(AgentUrl, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? 0;
            var downloadedBytes = 0L;

            await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
            await using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None);

            var buffer = new byte[8192];
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, ct)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                downloadedBytes += bytesRead;

                if (totalBytes > 0)
                {
                    var percent = (int)((downloadedBytes * 100) / totalBytes);
                    progressCallback?.Invoke($"Downloading agent... {downloadedBytes / 1024} KB", percent);
                }
            }

            await fileStream.FlushAsync(ct);
        }
        catch (Exception ex)
        {
            Logger.Error("DualAuth", $"Failed to download agent: {ex.Message}");
            if (File.Exists(tempPath)) File.Delete(tempPath);
            return new DualAuthResult { Success = false, Error = ex.Message };
        }

        // Validate the downloaded file
        var tempInfo = new FileInfo(tempPath);
        if (tempInfo.Length < MinAgentSizeBytes)
        {
            File.Delete(tempPath);
            var error = "Downloaded agent too small (corrupt or failed download)";
            Logger.Error("DualAuth", error);
            return new DualAuthResult { Success = false, Error = error };
        }

        // Move to final location
        if (File.Exists(agentPath)) File.Delete(agentPath);
        File.Move(tempPath, agentPath);

        progressCallback?.Invoke("DualAuth Agent ready", 100);
        Logger.Success("DualAuth", $"Agent downloaded successfully: {agentPath}");

        return new DualAuthResult { Success = true, AgentPath = agentPath };
    }

    /// <summary>
    /// Builds environment variables for DualAuth agent.
    /// </summary>
    /// <param name="agentPath">Full path to dualauth-agent.jar</param>
    /// <param name="authDomain">Custom auth domain (e.g., "sanasol.ws")</param>
    /// <param name="trustOfficialIssuers">Whether to also trust official Hytale issuers</param>
    /// <returns>Dictionary of environment variables to set</returns>
    public static Dictionary<string, string> BuildDualAuthEnvironment(
        string agentPath,
        string authDomain,
        bool trustOfficialIssuers = true)
    {
        var env = new Dictionary<string, string>
        {
            // Java agent flag - this tells the JVM to use the DualAuth agent
            ["JAVA_TOOL_OPTIONS"] = $"-javaagent:\"{agentPath}\"",
            
            // DualAuth configuration
            ["HYTALE_AUTH_DOMAIN"] = authDomain,
            ["HYTALE_TRUST_ALL_ISSUERS"] = "true",
            ["HYTALE_TRUST_OFFICIAL"] = trustOfficialIssuers ? "true" : "false",
        };

        Logger.Info("DualAuth", $"Environment configured: HYTALE_AUTH_DOMAIN={authDomain}");
        return env;
    }

    /// <summary>
    /// Applies DualAuth environment variables to a ProcessStartInfo.
    /// </summary>
    public static void ApplyToProcess(ProcessStartInfo startInfo, string agentPath, string authDomain, bool trustOfficialIssuers = true)
    {
        var env = BuildDualAuthEnvironment(agentPath, authDomain, trustOfficialIssuers);
        foreach (var (key, value) in env)
        {
            startInfo.Environment[key] = value;
        }
    }

    /// <summary>
    /// Builds environment variable lines for Unix launch scripts.
    /// </summary>
    public static string BuildUnixEnvLines(string agentPath, string authDomain, bool trustOfficialIssuers = true)
    {
        return $@"# DualAuth Agent Configuration
export JAVA_TOOL_OPTIONS=""-javaagent:{agentPath}""
export HYTALE_AUTH_DOMAIN=""{authDomain}""
export HYTALE_TRUST_ALL_ISSUERS=""true""
export HYTALE_TRUST_OFFICIAL=""{(trustOfficialIssuers ? "true" : "false")}""
";
    }
}

/// <summary>
/// Result of DualAuth agent operations.
/// </summary>
public class DualAuthResult
{
    public bool Success { get; init; }
    public string? AgentPath { get; init; }
    public bool AlreadyExists { get; init; }
    public string? Error { get; init; }
}
