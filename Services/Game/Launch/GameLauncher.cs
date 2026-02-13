using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using HyPrism.Models;
using HyPrism.Services.Core.Infrastructure;
using HyPrism.Services.Core.App;
using HyPrism.Services.Core.Integration;
using HyPrism.Services.Game.Asset;
using HyPrism.Services.Game.Auth;
using HyPrism.Services.Game.Instance;
using HyPrism.Services.User;

namespace HyPrism.Services.Game.Launch;

/// <summary>
/// Handles the game launch process including client patching, authentication,
/// process creation and monitoring, and Discord Rich Presence updates.
/// </summary>
/// <remarks>
/// Extracted from the former monolithic GameSessionService for better separation of concerns.
/// Coordinates between multiple services to prepare and launch the game.
/// </remarks>
public class GameLauncher : IGameLauncher
{
    private readonly IConfigService _configService;
    private readonly ILaunchService _launchService;
    private readonly IInstanceService _instanceService;
    private readonly IGameProcessService _gameProcessService;
    private readonly IProgressNotificationService _progressService;
    private readonly IDiscordService _discordService;
    private readonly ISkinService _skinService;
    private readonly IUserIdentityService _userIdentityService;
    private readonly AvatarService _avatarService;
    private readonly HttpClient _httpClient;
    private readonly HytaleAuthService _hytaleAuthService;
    
    private Config _config => _configService.Configuration;

    /// <summary>
    /// Stores the DualAuth agent path after download, used when building process start info.
    /// </summary>
    private string? _dualAuthAgentPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameLauncher"/> class.
    /// </summary>
    /// <param name="configService">Service for accessing configuration.</param>
    /// <param name="launchService">Service for launch prerequisites (JRE, VC++ Redist).</param>
    /// <param name="instanceService">Service for instance path management.</param>
    /// <param name="gameProcessService">Service for game process tracking.</param>
    /// <param name="progressService">Service for progress notifications.</param>
    /// <param name="discordService">Service for Discord Rich Presence.</param>
    /// <param name="skinService">Service for skin protection.</param>
    /// <param name="userIdentityService">Service for user identity management.</param>
    /// <param name="avatarService">Service for avatar backup.</param>
    /// <param name="httpClient">HTTP client for authentication requests.</param>
    /// <param name="hytaleAuthService">Service for official Hytale OAuth authentication.</param>
    public GameLauncher(
        IConfigService configService,
        ILaunchService launchService,
        IInstanceService instanceService,
        IGameProcessService gameProcessService,
        IProgressNotificationService progressService,
        IDiscordService discordService,
        ISkinService skinService,
        IUserIdentityService userIdentityService,
        AvatarService avatarService,
        HttpClient httpClient,
        HytaleAuthService hytaleAuthService)
    {
        _configService = configService;
        _launchService = launchService;
        _instanceService = instanceService;
        _gameProcessService = gameProcessService;
        _progressService = progressService;
        _discordService = discordService;
        _skinService = skinService;
        _userIdentityService = userIdentityService;
        _avatarService = avatarService;
        _httpClient = httpClient;
        _hytaleAuthService = hytaleAuthService;
        _gameProcessService.ProcessExited += OnGameProcessExited;
    }

    private void OnGameProcessExited(object? sender, EventArgs e)
    {
        try
        {
            Logger.Info("Game", "Game process exited, performing cleanup...");

            var uuid = _userIdentityService.GetUuidForUser(_config.Nick);
            _skinService.StopSkinProtection();
            _skinService.BackupProfileSkinData(uuid);
            
            // Copy the latest game avatar to persistent backup
            _avatarService.BackupAvatar(uuid);

            _discordService.SetPresence(DiscordService.PresenceState.Idle);
            _progressService.ReportGameStateChanged("stopped", 0);
        }
        catch (Exception ex)
        {
            Logger.Error("Game", $"Error during game exit cleanup: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task LaunchGameAsync(string versionPath, string branch, CancellationToken ct = default)
    {
        Logger.Info("Game", $"Preparing to launch from {versionPath}");

        // Validate profile/server compatibility before proceeding
        string sessionUuid = _userIdentityService.GetUuidForUser(_config.Nick);
        var currentProfile = _config.Profiles?.FirstOrDefault(p => p.UUID == sessionUuid);
        bool isOfficialProfile = currentProfile?.IsOfficial == true;

        if (IsOfficialServerMode() && !isOfficialProfile && _config.OnlineMode)
        {
            // The frontend should prevent this scenario by disabling the play button.
            // If we still get here (e.g. race condition), log a warning and continue
            // in offline mode rather than crashing.
            Logger.Warning("Game", "Official server mode with unofficial profile — falling back to offline mode");
        }

        var (executable, workingDir) = ResolveExecutablePaths(versionPath);

        if (!File.Exists(executable))
        {
            Logger.Error("Game", $"Game client not found at {executable}");
            throw new Exception($"Game client not found at {executable}");
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            string appBundle = Path.Combine(versionPath, "Client", "Hytale.app");
            UtilityService.ClearMacQuarantine(appBundle);
            Logger.Info("Game", "Cleared macOS quarantine attributes before patching");
        }

        ct.ThrowIfCancellationRequested();

        await PatchClientIfNeededAsync(versionPath);

        ct.ThrowIfCancellationRequested();

        _progressService.ReportDownloadProgress("launching", 0, "launch.detail.authenticating_generic", null, 0, 0);

        Logger.Info("Game", $"Using UUID for user '{_config.Nick}': {sessionUuid}");

        var (identityToken, sessionToken) = await AuthenticateAsync(sessionUuid);

        string javaPath = _launchService.GetJavaPath();
        if (!File.Exists(javaPath)) throw new Exception($"Java not found at {javaPath}");

        string userDataDir = _instanceService.GetInstanceUserDataPath(versionPath);
        Directory.CreateDirectory(userDataDir);

        RestoreProfileSkinData(sessionUuid, userDataDir);

        LogLaunchInfo(executable, javaPath, versionPath, userDataDir, sessionUuid);

        var startInfo = BuildProcessStartInfo(executable, workingDir, versionPath, userDataDir, javaPath, sessionUuid, identityToken, sessionToken);

        ct.ThrowIfCancellationRequested();

        await StartAndMonitorProcessAsync(startInfo, sessionUuid);
    }

    private static (string executable, string workingDir) ResolveExecutablePaths(string versionPath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return (
                Path.Combine(versionPath, "Client", "Hytale.app", "Contents", "MacOS", "HytaleClient"),
                Path.Combine(versionPath, "Client", "Hytale.app", "Contents", "MacOS")
            );
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return (
                Path.Combine(versionPath, "Client", "HytaleClient.exe"),
                Path.Combine(versionPath, "Client")
            );
        }

        return (
            Path.Combine(versionPath, "Client", "HytaleClient"),
            Path.Combine(versionPath, "Client")
        );
    }

    /// <summary>
    /// Determines whether the current AuthDomain setting points to official Hytale servers
    /// (i.e. no custom patching is needed).
    /// </summary>
    private bool IsOfficialServerMode()
    {
        var domain = _config.AuthDomain?.Trim();
        if (string.IsNullOrWhiteSpace(domain)) return false;

        // Known official domains / sentinel values
        return domain.Equals("official", StringComparison.OrdinalIgnoreCase)
            || domain.Contains("hytale.com", StringComparison.OrdinalIgnoreCase);
    }

    private async Task PatchClientIfNeededAsync(string versionPath)
    {
        // ── Official server mode: restore originals if previously patched ──
        if (IsOfficialServerMode())
        {
            bool clientPatched = ClientPatcher.IsClientPatched(versionPath);
            bool serverPatched = ClientPatcher.IsServerJarPatched(versionPath);

            if (clientPatched || serverPatched)
            {
                Logger.Info("Game", "Official server mode — restoring original (unpatched) binaries");
                _progressService.ReportDownloadProgress("patching", 0, "launch.detail.restoring_originals", null, 0, 0);

                try
                {
                    var restoreResult = ClientPatcher.RestoreAllFromBackup(versionPath, (msg, progress) =>
                    {
                        Logger.Info("Patcher", progress.HasValue ? $"{msg} ({progress}%)" : msg);
                        if (progress.HasValue)
                            _progressService.ReportDownloadProgress("patching", (int)progress.Value, msg, null, 0, 0);
                    });

                    if (restoreResult.Success)
                        Logger.Success("Game", "Originals restored — no patching needed for official servers");
                    else
                        Logger.Warning("Game", $"Restore had issues: {restoreResult.Error}");

                    _progressService.ReportDownloadProgress("patching", 100, "launch.detail.patching_complete", null, 0, 0);
                }
                catch (Exception ex)
                {
                    Logger.Warning("Game", $"Error restoring originals: {ex.Message}");
                }
            }
            else
            {
                Logger.Info("Game", "Official server mode — binaries are already unpatched, skipping");
            }

            return;
        }

        // ── Custom / default server mode: patch binaries ──
        if (string.IsNullOrWhiteSpace(_config.AuthDomain)) return;

        _progressService.ReportDownloadProgress("patching", 0, "launch.detail.patching_init", null, 0, 0);
        try
        {
            string baseDomain = _config.AuthDomain;
            if (baseDomain.StartsWith("sessions."))
            {
                baseDomain = baseDomain["sessions.".Length..];
            }

            Logger.Info("Game", $"Patching binary: hytale.com -> {baseDomain}");
            _progressService.ReportDownloadProgress("patching", 10, "launch.detail.patching_client", null, 0, 0);

            var patcher = new ClientPatcher(baseDomain);

            var patchResult = patcher.EnsureClientPatched(versionPath, (msg, progress) =>
            {
                Logger.Info("Patcher", progress.HasValue ? $"{msg} ({progress}%)" : msg);
                if (progress.HasValue)
                {
                    int mapped = 10 + (int)(progress.Value * 0.5);
                    _progressService.ReportDownloadProgress("patching", mapped, msg, null, 0, 0);
                }
            });

            // Use DualAuth agent instead of server JAR patching
            // DualAuth transforms bytecode at runtime, avoiding permanent JAR modifications
            Logger.Info("Game", $"Setting up DualAuth agent for auth domain: {baseDomain}");
            _progressService.ReportDownloadProgress("patching", 65, "launch.detail.dualauth_setup", null, 0, 0);

            try
            {
                var dualAuthResult = await DualAuthService.EnsureAgentAvailableAsync(versionPath, (msg, progress) =>
                {
                    Logger.Info("DualAuth", progress.HasValue ? $"{msg} ({progress}%)" : msg);
                    if (progress.HasValue)
                    {
                        int mapped = 65 + (int)(progress.Value * 0.25);
                        _progressService.ReportDownloadProgress("patching", mapped, msg, null, 0, 0);
                    }
                });

                if (dualAuthResult.Success)
                {
                    _dualAuthAgentPath = dualAuthResult.AgentPath;
                    Logger.Success("Game", $"DualAuth agent ready: {_dualAuthAgentPath}");
                }
                else
                {
                    Logger.Warning("Game", $"DualAuth agent setup failed: {dualAuthResult.Error}");
                    Logger.Warning("Game", "Server authentication may not work correctly without DualAuth");
                }
            }
            catch (Exception dualAuthEx)
            {
                Logger.Warning("Game", $"Error setting up DualAuth: {dualAuthEx.Message}");
            }

            if (patchResult.Success && patchResult.PatchCount > 0 && RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                try
                {
                    _progressService.ReportDownloadProgress("patching", 95, "launch.detail.resigning", null, 0, 0);
                    Logger.Info("Game", "Re-signing patched binary...");
                    string appBundle = Path.Combine(versionPath, "Client", "Hytale.app");
                    bool signed = ClientPatcher.SignMacOSBinary(appBundle);
                    if (signed) Logger.Success("Game", "Binary re-signed successfully");
                    else Logger.Warning("Game", "Binary signing failed - game may not launch");
                }
                catch (Exception signEx)
                {
                    Logger.Warning("Game", $"Error re-signing binary: {signEx.Message}");
                }
            }

            _progressService.ReportDownloadProgress("patching", 100, "launch.detail.patching_complete", null, 0, 0);

            // Force GC to reclaim the large byte[] arrays used during binary patching
            GC.Collect(2, GCCollectionMode.Aggressive, true, true);
            GC.WaitForPendingFinalizers();
        }
        catch (Exception ex)
        {
            Logger.Warning("Game", $"Error during binary patching: {ex.Message}");
            // Non-fatal, try to launch anyway
        }
    }

    private async Task<(string? identityToken, string? sessionToken)> AuthenticateAsync(string sessionUuid)
    {
        string? identityToken = null;
        string? sessionToken = null;

        // Check if the active profile is an official Hytale account
        var currentProfile = _config.Profiles?.FirstOrDefault(p => p.UUID == sessionUuid);
        bool isOfficialProfile = currentProfile?.IsOfficial == true;

        if (isOfficialProfile)
        {
            // Official Hytale account — use HytaleAuthService for OAuth tokens
            // Always create a fresh game session before launch to avoid SESSION EXPIRED errors
            _progressService.ReportDownloadProgress("launching", 20, "launch.detail.authenticating_official", null, 0, 0);
            Logger.Info("Game", "Official profile detected — refreshing tokens and creating fresh game session");

            try
            {
                // EnsureFreshSessionForLaunchAsync: refreshes access token if expired + always creates new game session
                var session = await _hytaleAuthService.EnsureFreshSessionForLaunchAsync();
                if (session == null)
                {
                    Logger.Warning("Game", "No valid Hytale session — attempting full re-authentication...");
                    _progressService.ReportDownloadProgress("launching", 25, "launch.detail.authenticating_browser", null, 0, 0);
                    session = await _hytaleAuthService.LoginAsync();
                    if (session == null)
                    {
                        Logger.Error("Game", "Full re-authentication failed — cannot launch in authenticated mode");
                        throw new Exception("Official Hytale session expired and re-login failed. Please try logging in again from the profile settings.");
                    }
                }

                identityToken = session.IdentityToken;
                sessionToken = session.SessionToken;

                if (!string.IsNullOrEmpty(identityToken))
                    Logger.Success("Game", "Official Hytale identity token obtained");
                else
                    Logger.Warning("Game", "Could not obtain Hytale session tokens — game may show SESSION EXPIRED");
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                Logger.Error("Game", $"Hytale auth error: {ex.Message}");
                throw;
            }

            return (identityToken, sessionToken);
        }

        // Non-official profile — use custom auth domain if configured
        if (!_config.OnlineMode || string.IsNullOrWhiteSpace(_config.AuthDomain))
            return (identityToken, sessionToken);

        _progressService.ReportDownloadProgress("launching", 20, "launch.detail.authenticating", [_config.AuthDomain], 0, 0);
        Logger.Info("Game", $"Online mode enabled - fetching auth tokens from {_config.AuthDomain}...");

        try
        {
            var authService = new AuthService(_httpClient, _config.AuthDomain);
            var tokenResult = await authService.GetGameSessionTokenAsync(sessionUuid, _config.Nick);

            if (tokenResult.Success && !string.IsNullOrEmpty(tokenResult.Token))
            {
                identityToken = tokenResult.Token;
                sessionToken = tokenResult.SessionToken ?? tokenResult.Token;
                Logger.Success("Game", "Identity token obtained successfully");
            }
            else
            {
                Logger.Warning("Game", $"Could not get auth token: {tokenResult.Error}");
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Game", $"Error fetching auth token: {ex.Message}");
        }

        return (identityToken, sessionToken);
    }

    private void RestoreProfileSkinData(string sessionUuid, string userDataDir)
    {
        var currentProfile = _config.Profiles?.FirstOrDefault(p => p.UUID == sessionUuid);
        if (currentProfile == null) return;

        _skinService.RestoreProfileSkinData(currentProfile);
        Logger.Info("Game", $"Restored skin data for profile '{currentProfile.Name}'");

        string skinCachePath = Path.Combine(userDataDir, "CachedPlayerSkins", $"{currentProfile.UUID}.json");
        if (File.Exists(skinCachePath))
        {
            _skinService.StartSkinProtection(currentProfile, skinCachePath);
        }
    }

    private void LogLaunchInfo(string executable, string javaPath, string gameDir, string userDataDir, string sessionUuid)
    {
        Logger.Info("Game", $"Launching: {executable}");
        Logger.Info("Game", $"Java: {javaPath}");
        Logger.Info("Game", $"AppDir: {gameDir}");
        Logger.Info("Game", $"UserData: {userDataDir}");
        Logger.Info("Game", $"Online Mode: {_config.OnlineMode}");
        Logger.Info("Game", $"Session UUID: {sessionUuid}");
    }

    private ProcessStartInfo BuildProcessStartInfo(
        string executable, string workingDir, string versionPath,
        string userDataDir, string javaPath, string sessionUuid,
        string? identityToken, string? sessionToken)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var startInfo = BuildWindowsStartInfo(executable, workingDir, versionPath, userDataDir, javaPath, sessionUuid, identityToken, sessionToken);
            ApplyGpuEnvironment(startInfo);
            ApplyDualAuthEnvironment(startInfo);
            return startInfo;
        }

        return BuildUnixStartInfo(executable, workingDir, versionPath, userDataDir, javaPath, sessionUuid, identityToken, sessionToken);
    }

    /// <summary>
    /// Applies DualAuth environment variables for custom auth server authentication.
    /// </summary>
    private void ApplyDualAuthEnvironment(ProcessStartInfo startInfo)
    {
        if (string.IsNullOrEmpty(_dualAuthAgentPath) || IsOfficialServerMode())
            return;

        string baseDomain = _config.AuthDomain ?? "";
        if (baseDomain.StartsWith("sessions."))
            baseDomain = baseDomain["sessions.".Length..];

        DualAuthService.ApplyToProcess(startInfo, _dualAuthAgentPath, baseDomain, trustOfficialIssuers: true);
        Logger.Info("Game", $"DualAuth environment applied to process");
    }

    /// <summary>
    /// Applies GPU environment variables to a ProcessStartInfo based on the configured GPU preference.
    /// Used for Windows direct-launch mode. Linux/macOS uses the launch script approach.
    /// </summary>
    private void ApplyGpuEnvironment(ProcessStartInfo startInfo)
    {
        var gpuPref = _config.GpuPreference?.ToLowerInvariant() ?? "dedicated";
        if (gpuPref == "auto") return;

        if (gpuPref == "dedicated")
        {
            // NVIDIA Optimus: request dedicated GPU
            startInfo.Environment["__NV_PRIME_RENDER_OFFLOAD"] = "1";
            startInfo.Environment["__GLX_VENDOR_LIBRARY_NAME"] = "nvidia";
            // AMD switchable graphics
            startInfo.Environment["DRI_PRIME"] = "1";
            // Windows: hint to driver to use high-performance GPU
            startInfo.Environment["DXGI_GPU_PREFERENCE"] = "2";
            Logger.Info("Game", "GPU preference: dedicated (NVIDIA/AMD env vars set)");
        }
        else if (gpuPref == "integrated")
        {
            startInfo.Environment["DRI_PRIME"] = "0";
            startInfo.Environment["__NV_PRIME_RENDER_OFFLOAD"] = "0";
            startInfo.Environment["DXGI_GPU_PREFERENCE"] = "1";
            Logger.Info("Game", "GPU preference: integrated (env vars set)");
        }
    }

    private ProcessStartInfo BuildWindowsStartInfo(
        string executable, string workingDir, string gameDir,
        string userDataDir, string javaPath, string sessionUuid,
        string? identityToken, string? sessionToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            WorkingDirectory = workingDir,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        startInfo.ArgumentList.Add("--app-dir");
        startInfo.ArgumentList.Add(gameDir);
        startInfo.ArgumentList.Add("--user-dir");
        startInfo.ArgumentList.Add(userDataDir);
        startInfo.ArgumentList.Add("--java-exec");
        startInfo.ArgumentList.Add(javaPath);
        startInfo.ArgumentList.Add("--name");
        startInfo.ArgumentList.Add(_config.Nick);

        if (_config.OnlineMode && !string.IsNullOrEmpty(identityToken) && !string.IsNullOrEmpty(sessionToken))
        {
            startInfo.ArgumentList.Add("--auth-mode");
            startInfo.ArgumentList.Add("authenticated");
            startInfo.ArgumentList.Add("--uuid");
            startInfo.ArgumentList.Add(sessionUuid);
            startInfo.ArgumentList.Add("--identity-token");
            startInfo.ArgumentList.Add(identityToken);
            startInfo.ArgumentList.Add("--session-token");
            startInfo.ArgumentList.Add(sessionToken);
            Logger.Info("Game", $"Using authenticated mode with session UUID: {sessionUuid}");
        }
        else
        {
            startInfo.ArgumentList.Add("--auth-mode");
            startInfo.ArgumentList.Add("offline");
            startInfo.ArgumentList.Add("--uuid");
            startInfo.ArgumentList.Add(sessionUuid);
            Logger.Info("Game", $"Using offline mode with UUID: {sessionUuid}");
        }

        Logger.Info("Game", $"Windows launch args: {string.Join(" ", startInfo.ArgumentList)}");
        return startInfo;
    }

    private ProcessStartInfo BuildUnixStartInfo(
        string executable, string workingDir, string versionPath,
        string userDataDir, string javaPath, string sessionUuid,
        string? identityToken, string? sessionToken)
    {
        var gameArgs = new List<string>
        {
            $"--app-dir \"{versionPath}\"",
            $"--user-dir \"{userDataDir}\"",
            $"--java-exec \"{javaPath}\"",
            $"--name \"{_config.Nick}\""
        };

        if (_config.OnlineMode && !string.IsNullOrEmpty(identityToken) && !string.IsNullOrEmpty(sessionToken))
        {
            gameArgs.Add("--auth-mode authenticated");
            gameArgs.Add($"--uuid \"{sessionUuid}\"");
            gameArgs.Add($"--identity-token \"{identityToken}\"");
            gameArgs.Add($"--session-token \"{sessionToken}\"");
            Logger.Info("Game", $"Using authenticated mode with session UUID: {sessionUuid}");
        }
        else
        {
            gameArgs.Add("--auth-mode offline");
            gameArgs.Add($"--uuid \"{sessionUuid}\"");
            Logger.Info("Game", $"Using offline mode with UUID: {sessionUuid}");
        }

        string argsString = string.Join(" ", gameArgs);
        string launchScript = Path.Combine(versionPath, "launch.sh");
        string homeDir = Environment.GetEnvironmentVariable("HOME") ?? "/Users/" + Environment.UserName;
        string userName = Environment.GetEnvironmentVariable("USER") ?? Environment.UserName;
        string clientDir = Path.Combine(versionPath, "Client");

        string scriptContent = $@"#!/bin/bash
# Launch script generated by HyPrism
# Uses env to clear ALL environment variables before launching game

# Set LD_LIBRARY_PATH to include Client directory for shared libraries
CLIENT_DIR=""{clientDir}""

{BuildGpuEnvLines()}{BuildDualAuthEnvLines()}exec env \
    HOME=""{homeDir}"" \
    USER=""{userName}"" \
    PATH=""/usr/bin:/bin:/usr/sbin:/sbin:/usr/local/bin"" \
    SHELL=""/bin/zsh"" \
    TMPDIR=""{Path.GetTempPath().TrimEnd('/')}"" \
    LD_LIBRARY_PATH=""$CLIENT_DIR:$LD_LIBRARY_PATH"" \
    $DUALAUTH_ENV \
    ""{executable}"" {argsString}
";
        File.WriteAllText(launchScript, scriptContent);

        using var chmod = Process.Start(new ProcessStartInfo
        {
            FileName = "/bin/chmod",
            Arguments = $"+x \"{launchScript}\"",
            UseShellExecute = false,
            CreateNoWindow = true
        });
        chmod?.WaitForExit();

        var startInfo = new ProcessStartInfo
        {
            FileName = "/bin/bash",
            WorkingDirectory = workingDir,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        startInfo.ArgumentList.Add(launchScript);

        Logger.Info("Game", $"Launch script: {launchScript}");
        return startInfo;
    }

    /// <summary>
    /// Builds GPU environment variable lines for the Unix launch script.
    /// Returns a string with export lines to be placed before 'exec env'.
    /// </summary>
    private string BuildGpuEnvLines()
    {
        var gpuPref = _config.GpuPreference?.ToLowerInvariant() ?? "dedicated";
        if (gpuPref == "auto") return "";

        if (gpuPref == "dedicated")
        {
            Logger.Info("Game", "GPU preference: dedicated (NVIDIA/AMD env vars in launch script)");
            return @"# GPU preference: dedicated (discrete GPU)
export __NV_PRIME_RENDER_OFFLOAD=1
export __GLX_VENDOR_LIBRARY_NAME=nvidia
export DRI_PRIME=1

";
        }

        if (gpuPref == "integrated")
        {
            Logger.Info("Game", "GPU preference: integrated");
            return @"# GPU preference: integrated
export DRI_PRIME=0
export __NV_PRIME_RENDER_OFFLOAD=0

";
        }

        return "";
    }

    /// <summary>
    /// Builds DualAuth environment variable lines for the Unix launch script.
    /// Returns a string with export lines to be placed before 'exec env'.
    /// </summary>
    private string BuildDualAuthEnvLines()
    {
        if (string.IsNullOrEmpty(_dualAuthAgentPath) || IsOfficialServerMode())
            return "# No DualAuth (official server mode or agent unavailable)\nDUALAUTH_ENV=\"\"\n\n";

        string baseDomain = _config.AuthDomain ?? "";
        if (baseDomain.StartsWith("sessions."))
            baseDomain = baseDomain["sessions.".Length..];

        Logger.Info("Game", $"DualAuth env lines for Unix script: {baseDomain}");
        
        return $@"# DualAuth Agent Configuration
export JAVA_TOOL_OPTIONS=""-javaagent:{_dualAuthAgentPath}""
export HYTALE_AUTH_DOMAIN=""{baseDomain}""
export HYTALE_TRUST_ALL_ISSUERS=""true""
export HYTALE_TRUST_OFFICIAL=""true""
DUALAUTH_ENV=""JAVA_TOOL_OPTIONS=$JAVA_TOOL_OPTIONS HYTALE_AUTH_DOMAIN=$HYTALE_AUTH_DOMAIN HYTALE_TRUST_ALL_ISSUERS=$HYTALE_TRUST_ALL_ISSUERS HYTALE_TRUST_OFFICIAL=$HYTALE_TRUST_OFFICIAL""

";
    }

    private async Task StartAndMonitorProcessAsync(ProcessStartInfo startInfo, string sessionUuid)
    {

        Process? process = null;
        try
        {
            _progressService.ReportDownloadProgress("launching", 80, "launch.detail.starting_process", null, 0, 0);

            process = new Process { StartInfo = startInfo };
            var interfaceLoadedTcs = new TaskCompletionSource<bool>();

            var sysInfoBuffer = new List<string>();
            bool capturingSysInfo = false;
            bool capturingAudio = false;

            process.OutputDataReceived += (sender, e) =>
            {
                if (string.IsNullOrEmpty(e.Data)) return;
                string line = e.Data;
                bool isNewLogEntry = Regex.IsMatch(line, @"^\d{4}-\d{2}-\d{2}");

                if (line.StartsWith("Set log path to")) { Logger.Info("Game", line); return; }

                if (line.Trim() == "System informations" || line.Contains("|System informations"))
                { capturingSysInfo = true; return; }

                if (capturingSysInfo)
                {
                    if (isNewLogEntry) { capturingSysInfo = false; }
                    else
                    {
                        string trimmed = line.Trim();
                        if (trimmed.StartsWith("OpenGL") || trimmed.StartsWith("GPU"))
                        { sysInfoBuffer.Add(trimmed); return; }
                    }
                }

                if (line.Contains("|Audio:")) { capturingAudio = true; return; }

                if (capturingAudio)
                {
                    if (isNewLogEntry)
                    {
                        capturingAudio = false;
                        Logger.Info("Game", "Got system info");
                        foreach (var sysLine in sysInfoBuffer) Logger.Info("Game", $"\t{sysLine}");
                        sysInfoBuffer.Clear();
                    }
                    else
                    {
                        string trimmed = line.Trim();
                        if (trimmed.StartsWith("OpenAL") || trimmed.StartsWith("Renderer") ||
                            trimmed.StartsWith("Vendor") || trimmed.StartsWith("Using device"))
                        { sysInfoBuffer.Add(trimmed); }
                        return;
                    }
                }

                if (line.Contains("|INFO|HytaleClient.Application.AppStartup|Interface loaded.") ||
                    line.Contains("Interface loaded."))
                {
                    Logger.Success("Game", "Started successfully");
                    interfaceLoadedTcs.TrySetResult(true);
                }
            };

            process.ErrorDataReceived += (_, _) => { };

            if (!process.Start())
            {
                Logger.Error("Game", "Process.Start returned false - game failed to launch");
                _progressService.ReportError("launch", "Failed to start game", "Process.Start returned false");
                throw new Exception("Failed to start game process");
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Transfer ownership to GameProcessService (it will handle disposal and notify subscribers)
            _gameProcessService.SetGameProcess(process);
            Logger.Success("Game", $"Game started with PID: {process.Id}");

            _discordService.SetPresence(DiscordService.PresenceState.Playing, $"Playing as {_config.Nick}");
            _progressService.ReportGameStateChanged("started", process.Id);
            _progressService.ReportDownloadProgress("launching", 100, "launch.detail.waiting_for_window", null, 0, 0);

            // Wait for interface loaded signal or timeout (60s)
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(60));
            var completedTask = await Task.WhenAny(interfaceLoadedTcs.Task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                Logger.Warning("Game", "Timed out waiting for interface load signal (or game output is silent)");
            }

            _progressService.ReportDownloadProgress("complete", 100, "launch.detail.done", null, 0, 0);
        }
        catch (Exception ex)
        {
            Logger.Error("Game", $"Failed to start game process: {ex.Message}");
            
            // Cleanup process if failed before transferring to GameProcessService
            if (process != null && _gameProcessService.GetGameProcess() != process)
            {
                try { process.Dispose(); } catch { }
            }
            
            _progressService.ReportError("launch", "Failed to start game", ex.Message);
            throw new Exception($"Failed to start game: {ex.Message}");
        }
    }
}
