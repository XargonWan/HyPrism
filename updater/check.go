package updater

import (
	"HyPrism/internal/util/download"
	"context"
	"encoding/json"
	"fmt"
	"os"
	"runtime"
	"strings"
)

const versionJSONAsset = "version.json"

// UpdateInfo represents the update information
type UpdateInfo struct {
	Version string `json:"version"`
	Linux   struct {
		Amd64 struct {
			Launcher Asset `json:"launcher"`
		} `json:"amd64"`
	} `json:"linux"`
	Windows struct {
		Amd64 struct {
			Launcher Asset `json:"launcher"`
		} `json:"amd64"`
	} `json:"windows"`
	Darwin struct {
		Amd64 struct {
			Launcher Asset `json:"launcher"`
		} `json:"amd64"`
		Arm64 struct {
			Launcher Asset `json:"launcher"`
		} `json:"arm64"`
	} `json:"darwin"`
}

// Asset represents a downloadable asset
type Asset struct {
	URL    string `json:"url"`
	Sha256 string `json:"sha256"`
}

// CheckUpdate checks for launcher updates
func CheckUpdate(ctx context.Context, current string) (*Asset, string, error) {
	info, err := fetchUpdateInfo(ctx)
	if err != nil {
		return nil, "", err
	}

	currentClean := strings.TrimPrefix(strings.TrimSpace(current), "v")
	latestClean := strings.TrimPrefix(strings.TrimSpace(info.Version), "v")

	fmt.Printf("Current version: %s, Latest version: %s\n", current, info.Version)

	if currentClean == latestClean {
		fmt.Println("Already on latest version")
		return nil, "", nil
	}

	var asset *Asset
	switch runtime.GOOS {
	case "windows":
		asset = &info.Windows.Amd64.Launcher
		fmt.Printf("Update available for Windows: %s -> %s\n", current, info.Version)
	case "darwin":
		if runtime.GOARCH == "arm64" {
			asset = &info.Darwin.Arm64.Launcher
		} else {
			asset = &info.Darwin.Amd64.Launcher
		}
		fmt.Printf("Update available for macOS: %s -> %s\n", current, info.Version)
	default:
		asset = &info.Linux.Amd64.Launcher
		fmt.Printf("Update available for Linux: %s -> %s\n", current, info.Version)
	}

	if asset.URL == "" {
		return nil, "", fmt.Errorf("no download URL found for %s/%s", runtime.GOOS, runtime.GOARCH)
	}

	return asset, info.Version, nil
}

// GetHelperAsset is deprecated - updates now use built-in scripts
func GetHelperAsset(ctx context.Context) (*Asset, error) {
	return nil, fmt.Errorf("helper-based updates are no longer used")
}

func fetchUpdateInfo(ctx context.Context) (*UpdateInfo, error) {
	tmpFile, err := os.CreateTemp("", "version-*.json")
	if err != nil {
		return nil, err
	}
	tmpPath := tmpFile.Name()
	tmpFile.Close()
	defer os.Remove(tmpPath)

	// Download version.json from releases
	err = download.DownloadLatestReleaseAsset(ctx, versionJSONAsset, tmpPath, nil)
	if err != nil {
		return nil, fmt.Errorf("failed to fetch update info: %w", err)
	}

	data, err := os.ReadFile(tmpPath)
	if err != nil {
		return nil, err
	}

	var info UpdateInfo
	if err := json.Unmarshal(data, &info); err != nil {
		return nil, fmt.Errorf("failed to parse update info: %w", err)
	}

	return &info, nil
}
