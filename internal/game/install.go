package game

import (
	"context"
	"fmt"
	"os"
	"path/filepath"
	"runtime"
	"strconv"
	"sync"

	"HyPrism/internal/env"
	"HyPrism/internal/java"
	"HyPrism/internal/pwr"
	"HyPrism/internal/pwr/butler"
)

var (
	installMutex sync.Mutex
	isInstalling bool
)

// EnsureInstalled ensures the game is installed and up to date
func EnsureInstalled(ctx context.Context, progress func(stage string, progress float64, msg string, file string, speed string, down, total int64)) error {
	// Prevent multiple simultaneous installations
	installMutex.Lock()
	if isInstalling {
		installMutex.Unlock()
		return fmt.Errorf("installation already in progress")
	}
	isInstalling = true
	installMutex.Unlock()

	defer func() {
		installMutex.Lock()
		isInstalling = false
		installMutex.Unlock()
	}()

	// Download JRE
	if err := java.DownloadJRE(ctx, progress); err != nil {
		return fmt.Errorf("failed to download Java Runtime: %w", err)
	}

	// Install Butler (required for PWR patch extraction)
	if _, err := butler.InstallButler(ctx, progress); err != nil {
		return fmt.Errorf("failed to install Butler tool: %w", err)
	}

	// Find latest version with details
	if progress != nil {
		progress("version", 0, "Checking for game updates...", "", "", 0, 0)
	}

	result := pwr.FindLatestVersionWithDetails("release")

	if result.Error != nil {
		return fmt.Errorf(
			"cannot find game versions on server\n\n"+
				"Platform: %s %s\n"+
				"Error: %v\n\n"+
				"Troubleshooting:\n"+
				"• Ensure your system is supported (Windows/Linux/macOS)\n"+
				"• Check if game is available for your architecture\n"+
				"• Verify firewall allows connections to game-patches.hytale.com\n"+
				"• Try disabling VPN temporarily\n\n"+
				"Checked URLs: %d\n"+
				"Sample: %s",
			runtime.GOOS,
			runtime.GOARCH,
			result.Error,
			len(result.CheckedURLs),
			getFirstURL(result.CheckedURLs),
		)
	}

	if result.LatestVersion == 0 {
		return fmt.Errorf(
			"no game versions found for your platform\n\n"+
				"Platform: %s %s\n"+
				"This may mean the game is not yet available for your system.\n"+
				"Check https://hytale.com for supported platforms.",
			runtime.GOOS,
			runtime.GOARCH,
		)
	}

	if progress != nil {
		progress("version", 100, fmt.Sprintf("Latest version: %d", result.LatestVersion), "", "", 0, 0)
	}

	// Install/update the game
	if err := InstallGame(ctx, "release", result.LatestVersion, progress); err != nil {
		return fmt.Errorf("failed to install game: %w", err)
	}

	return nil
}

// InstallGame installs or updates the game to a specific version
func InstallGame(ctx context.Context, versionType string, remoteVer int, progressCallback func(stage string, progress float64, message string, currentFile string, speed string, downloaded, total int64)) error {
	localStr := pwr.GetLocalVersion()
	local, _ := strconv.Atoi(localStr)

	gameLatestDir := filepath.Join(env.GetDefaultAppDir(), "release", "package", "game", "latest")

	// Determine client path based on OS (matching TEMPLATE.sh structure)
	var clientPath string
	switch runtime.GOOS {
	case "darwin":
		clientPath = filepath.Join(gameLatestDir, "Client", "Hytale.app", "Contents", "MacOS", "HytaleClient")
	case "windows":
		clientPath = filepath.Join(gameLatestDir, "Client", "HytaleClient.exe")
	default:
		clientPath = filepath.Join(gameLatestDir, "Client", "HytaleClient")
	}
	_, clientErr := os.Stat(clientPath)

	if local == remoteVer && clientErr == nil {
		if progressCallback != nil {
			progressCallback("complete", 100, "Game is up to date", "", "", 0, 0)
		}
		return nil
	}

	prevVer := local
	if clientErr != nil {
		prevVer = 0
		if progressCallback != nil {
			progressCallback("download", 0, fmt.Sprintf("Installing game version %d...", remoteVer), "", "", 0, 0)
		}
	} else {
		if progressCallback != nil {
			progressCallback("download", 0, fmt.Sprintf("Updating from version %d to %d...", local, remoteVer), "", "", 0, 0)
		}
	}

	// Create installation marker
	markerFile := filepath.Join(gameLatestDir, ".installing")
	os.WriteFile(markerFile, []byte(fmt.Sprintf("%d", remoteVer)), 0644)
	defer os.Remove(markerFile)

	// Download the patch file
	pwrPath, err := pwr.DownloadPWR(ctx, versionType, prevVer, remoteVer, progressCallback)
	if err != nil {
		return fmt.Errorf("failed to download game patch: %w", err)
	}

	// Verify the patch file exists and is readable
	info, err := os.Stat(pwrPath)
	if err != nil {
		return fmt.Errorf("patch file not accessible: %w", err)
	}

	fmt.Printf("Patch file size: %d bytes\n", info.Size())

	// Apply the patch
	if progressCallback != nil {
		progressCallback("install", 0, "Applying game patch...", "", "", 0, 0)
	}

	if err := pwr.ApplyPWR(ctx, pwrPath, progressCallback); err != nil {
		return fmt.Errorf("failed to apply game patch: %w", err)
	}

	// Verify installation
	if _, err := os.Stat(clientPath); err != nil {
		return fmt.Errorf("game installation incomplete: client executable not found at %s", clientPath)
	}

	// Save the new version
	if err := pwr.SaveLocalVersion(remoteVer); err != nil {
		fmt.Printf("Warning: failed to save version info: %v\n", err)
	}

	if progressCallback != nil {
		progressCallback("complete", 100, "Game installed successfully", "", "", 0, 0)
	}

	return nil
}

// EnsureInstalledVersion ensures a specific version type (release/prerelease) is installed
func EnsureInstalledVersion(ctx context.Context, versionType string, progress func(stage string, progress float64, msg string, file string, speed string, down, total int64)) error {
	// Prevent multiple simultaneous installations
	installMutex.Lock()
	if isInstalling {
		installMutex.Unlock()
		return fmt.Errorf("installation already in progress")
	}
	isInstalling = true
	installMutex.Unlock()

	defer func() {
		installMutex.Lock()
		isInstalling = false
		installMutex.Unlock()
	}()

	// Download JRE
	if err := java.DownloadJRE(ctx, progress); err != nil {
		return fmt.Errorf("failed to download Java Runtime: %w", err)
	}

	// Install Butler
	if _, err := butler.InstallButler(ctx, progress); err != nil {
		return fmt.Errorf("failed to install Butler tool: %w", err)
	}

	// Find latest version for this type
	if progress != nil {
		progress("version", 0, fmt.Sprintf("Checking for %s updates...", versionType), "", "", 0, 0)
	}

	result := pwr.FindLatestVersionWithDetails(versionType)

	if result.Error != nil {
		return fmt.Errorf("cannot find %s versions: %v", versionType, result.Error)
	}

	if result.LatestVersion == 0 {
		return fmt.Errorf("no %s versions found for your platform", versionType)
	}

	if progress != nil {
		progress("version", 100, fmt.Sprintf("Latest %s version: %d", versionType, result.LatestVersion), "", "", 0, 0)
	}

	// Install the game
	if err := InstallGame(ctx, versionType, result.LatestVersion, progress); err != nil {
		return fmt.Errorf("failed to install game: %w", err)
	}

	return nil
}

// EnsureInstalledVersionSpecific ensures a specific branch AND version is installed
func EnsureInstalledVersionSpecific(ctx context.Context, versionType string, version int, progress func(stage string, progress float64, msg string, file string, speed string, down, total int64)) error {
	// Prevent multiple simultaneous installations
	installMutex.Lock()
	if isInstalling {
		installMutex.Unlock()
		return fmt.Errorf("installation already in progress")
	}
	isInstalling = true
	installMutex.Unlock()

	defer func() {
		installMutex.Lock()
		isInstalling = false
		installMutex.Unlock()
	}()

	// Check if this specific version is already installed in instance folder
	instanceGameDir := env.GetInstanceGameDir(versionType, version)
	var clientPath string
	switch runtime.GOOS {
	case "darwin":
		clientPath = filepath.Join(instanceGameDir, "Client", "Hytale.app", "Contents", "MacOS", "HytaleClient")
	case "windows":
		clientPath = filepath.Join(instanceGameDir, "Client", "HytaleClient.exe")
	default:
		clientPath = filepath.Join(instanceGameDir, "Client", "HytaleClient")
	}

	if _, err := os.Stat(clientPath); err == nil {
		fmt.Printf("Instance %s v%d already installed at %s\n", versionType, version, instanceGameDir)
		if progress != nil {
			progress("complete", 100, fmt.Sprintf("%s v%d is ready", versionType, version), "", "", 0, 0)
		}
		return nil
	}

	// Download JRE
	if err := java.DownloadJRE(ctx, progress); err != nil {
		return fmt.Errorf("failed to download Java Runtime: %w", err)
	}

	// Install Butler
	if _, err := butler.InstallButler(ctx, progress); err != nil {
		return fmt.Errorf("failed to install Butler tool: %w", err)
	}

	if progress != nil {
		progress("download", 0, fmt.Sprintf("Installing %s v%d...", versionType, version), "", "", 0, 0)
	}

	// Create instance folders
	if err := env.CreateInstanceFolders(versionType, version); err != nil {
		return fmt.Errorf("failed to create instance folders: %w", err)
	}

	// Install to instance-specific directory
	if err := InstallGameToInstance(ctx, versionType, version, progress); err != nil {
		return fmt.Errorf("failed to install game: %w", err)
	}

	return nil
}

// InstallGameToInstance installs the game to an instance-specific directory
func InstallGameToInstance(ctx context.Context, versionType string, version int, progressCallback func(stage string, progress float64, message string, currentFile string, speed string, downloaded, total int64)) error {
	instanceGameDir := env.GetInstanceGameDir(versionType, version)

	// Download the patch file
	pwrPath, err := pwr.DownloadPWR(ctx, versionType, 0, version, progressCallback)
	if err != nil {
		return fmt.Errorf("failed to download game patch: %w", err)
	}

	// Verify the patch file exists
	info, err := os.Stat(pwrPath)
	if err != nil {
		return fmt.Errorf("patch file not accessible: %w", err)
	}
	fmt.Printf("Patch file size: %d bytes\n", info.Size())

	// Apply the patch to instance directory
	if progressCallback != nil {
		progressCallback("install", 0, "Installing game...", "", "", 0, 0)
	}

	if err := pwr.ApplyPWRToDir(ctx, pwrPath, instanceGameDir, progressCallback); err != nil {
		return fmt.Errorf("failed to apply game patch: %w", err)
	}

	// Verify installation
	var clientPath string
	switch runtime.GOOS {
	case "darwin":
		clientPath = filepath.Join(instanceGameDir, "Client", "Hytale.app", "Contents", "MacOS", "HytaleClient")
	case "windows":
		clientPath = filepath.Join(instanceGameDir, "Client", "HytaleClient.exe")
	default:
		clientPath = filepath.Join(instanceGameDir, "Client", "HytaleClient")
	}

	if _, err := os.Stat(clientPath); err != nil {
		return fmt.Errorf("installation incomplete: client not found at %s", clientPath)
	}

	// Save version marker in instance directory
	versionFile := filepath.Join(env.GetInstanceDir(versionType, version), "version.txt")
	os.WriteFile(versionFile, []byte(fmt.Sprintf("%d", version)), 0644)

	if progressCallback != nil {
		progressCallback("complete", 100, fmt.Sprintf("%s v%d installed successfully", versionType, version), "", "", 0, 0)
	}

	return nil
}

func getFirstURL(urls []string) string {
	if len(urls) == 0 {
		return "none"
	}
	return urls[0]
}
