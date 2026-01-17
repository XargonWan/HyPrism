package pwr

import (
	"context"
	"fmt"
	"os"
	"os/exec"
	"path/filepath"
	"runtime"
	"strings"
	"time"

	"HyPrism/internal/env"
	"HyPrism/internal/pwr/butler"
)

// cleanStagingDirectory removes staging directory and any leftover temp files
// This fixes "Access Denied" errors on Windows where previous installations left locked files
func cleanStagingDirectory(gameDir string) error {
	stagingDir := filepath.Join(gameDir, "staging-temp")
	
	// Remove staging directory completely
	if err := os.RemoveAll(stagingDir); err != nil {
		// On Windows, try to remove files individually if directory removal fails
		if runtime.GOOS == "windows" {
			filepath.Walk(stagingDir, func(path string, info os.FileInfo, err error) error {
				if err == nil && !info.IsDir() {
					os.Remove(path)
				}
				return nil
			})
			// Try again after individual file removal
			os.RemoveAll(stagingDir)
		}
	}
	
	// Also clean any .tmp files in game directory that butler might have left
	entries, _ := os.ReadDir(gameDir)
	for _, entry := range entries {
		name := entry.Name()
		if strings.HasSuffix(name, ".tmp") || strings.HasPrefix(name, "sf-") {
			os.Remove(filepath.Join(gameDir, name))
		}
	}
	
	return nil
}

// ApplyPWR applies a PWR patch file using Butler (itch.io patching tool)
// PWR files are NOT regular zip files - they require Butler to extract
func ApplyPWR(ctx context.Context, pwrFile string, progressCallback func(stage string, progress float64, message string, currentFile string, speed string, downloaded, total int64)) error {
	gameDir := filepath.Join(env.GetDefaultAppDir(), "release", "package", "game", "latest")
	stagingDir := filepath.Join(gameDir, "staging-temp")
	
	// Check if game is already installed
	// Determine client path based on OS (matching TEMPLATE.sh structure)
	var clientPath string
	switch runtime.GOOS {
	case "darwin":
		clientPath = filepath.Join(gameDir, "Client", "Hytale.app", "Contents", "MacOS", "HytaleClient")
	case "windows":
		clientPath = filepath.Join(gameDir, "Client", "HytaleClient.exe")
	default:
		clientPath = filepath.Join(gameDir, "Client", "HytaleClient")
	}
	
	if _, err := os.Stat(clientPath); err == nil {
		fmt.Println("Game files detected, skipping patch installation")
		if progressCallback != nil {
			progressCallback("install", 100, "Game already installed", "", "", 0, 0)
		}
		// Clean up patch file
		go func() {
			time.Sleep(1 * time.Second)
			os.Remove(pwrFile)
		}()
		return nil
	}
	
	// Get Butler path
	butlerPath, err := butler.GetButlerPath()
	if err != nil {
		return fmt.Errorf("butler not found: %w", err)
	}
	
	// IMPORTANT: Clean staging directory BEFORE creating it
	// This fixes "Access Denied" errors on Windows from leftover files
	if progressCallback != nil {
		progressCallback("install", 0, "Preparing installation...", "", "", 0, 0)
	}
	cleanStagingDirectory(gameDir)
	
	// Create directories
	if err := os.MkdirAll(gameDir, 0755); err != nil {
		return fmt.Errorf("failed to create game directory: %w", err)
	}
	if err := os.MkdirAll(stagingDir, 0755); err != nil {
		return fmt.Errorf("failed to create staging directory: %w", err)
	}

	if progressCallback != nil {
		progressCallback("install", 5, "Installing Hytale...", "", "", 0, 0)
	}

	fmt.Printf("Applying PWR patch with Butler: %s\n", pwrFile)
	fmt.Printf("Butler path: %s\n", butlerPath)
	fmt.Printf("Game directory: %s\n", gameDir)
	
	// Run butler apply with staging directory (like Hytale-F2P does)
	// Add --no-save-interval to avoid checkpoint file issues on Windows
	var cmd *exec.Cmd
	if runtime.GOOS == "windows" {
		// On Windows, disable save interval to avoid checkpoint rename issues
		cmd = exec.CommandContext(ctx, butlerPath, "apply", "--staging-dir", stagingDir, "--no-save-interval", pwrFile, gameDir)
	} else {
		cmd = exec.CommandContext(ctx, butlerPath, "apply", "--staging-dir", stagingDir, pwrFile, gameDir)
	}
	
	output, err := cmd.CombinedOutput()
	if err != nil {
		fmt.Printf("Butler error output: %s\n", string(output))
		
		// If it failed, try to clean up and provide helpful message
		cleanStagingDirectory(gameDir)
		
		errMsg := string(output)
		if strings.Contains(errMsg, "Acceso denegado") || strings.Contains(errMsg, "Access denied") || strings.Contains(errMsg, "access is denied") {
			return fmt.Errorf("installation failed: file access denied\n\n"+
				"This usually happens when:\n"+
				"• The game is currently running - please close it\n"+
				"• Antivirus is blocking the installation - try disabling it temporarily\n"+
				"• Previous installation was interrupted - restart the launcher\n\n"+
				"Try: Close the launcher, delete the folder:\n"+
				"%%LOCALAPPDATA%%\\HyPrism\\release\\package\\game\\latest\n"+
				"Then restart the launcher.\n\n"+
				"Technical: %w\nOutput: %s", err, errMsg)
		}
		
		return fmt.Errorf("butler apply failed: %w\nOutput: %s", err, errMsg)
	}

	fmt.Printf("Butler output: %s\n", string(output))

	// Clean up staging directory
	cleanStagingDirectory(gameDir)

	// Clean up patch file
	go func() {
		time.Sleep(2 * time.Second)
		os.Remove(pwrFile)
	}()

	if progressCallback != nil {
		progressCallback("install", 100, "Hytale installed successfully", "", "", 0, 0)
	}

	// Set executable permissions on Unix
	if runtime.GOOS != "windows" {
		os.Chmod(clientPath, 0755)
	}

	fmt.Println("Installation complete")
	return nil
}
