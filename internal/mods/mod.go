package mods

import (
	"encoding/json"
	"fmt"
	"os"
	"path/filepath"

	"HyPrism/internal/env"
)

// Mod represents a mod
type Mod struct {
	ID           string `json:"id"`
	Name         string `json:"name"`
	Slug         string `json:"slug,omitempty"`
	Version      string `json:"version"`
	Author       string `json:"author"`
	Description  string `json:"description"`
	DownloadURL  string `json:"downloadUrl,omitempty"`
	CurseForgeID int    `json:"curseForgeId,omitempty"`
	FileID       int    `json:"fileId,omitempty"`
	Enabled      bool   `json:"enabled"`
	InstalledAt  string `json:"installedAt"`  // ISO 8601 format
	UpdatedAt    string `json:"updatedAt"`    // ISO 8601 format
	FilePath     string `json:"filePath"`
	IconURL      string `json:"iconUrl,omitempty"`
	Downloads    int    `json:"downloads,omitempty"`
	Category     string `json:"category,omitempty"`
	LatestVersion string `json:"latestVersion,omitempty"`
	LatestFileID  int    `json:"latestFileId,omitempty"`
}

// ModManifest stores installed mods info
type ModManifest struct {
	Mods    []Mod  `json:"mods"`
	Version string `json:"version"`
}

// GetModsDir returns the mods directory path (legacy - for backwards compatibility)
// Mods should be in UserData/Mods as that's where the game reads them
func GetModsDir() string {
	return filepath.Join(env.GetDefaultAppDir(), "UserData", "Mods")
}

// GetInstanceModsDir returns the mods directory for a specific instance
func GetInstanceModsDir(branch string, version int) string {
	return filepath.Join(env.GetInstanceUserDataDir(branch, version), "Mods")
}

// GetModManifestPath returns the mod manifest path (legacy)
func GetModManifestPath() string {
	return filepath.Join(GetModsDir(), "manifest.json")
}

// GetInstanceModManifestPath returns the mod manifest path for a specific instance
func GetInstanceModManifestPath(branch string, version int) string {
	return filepath.Join(GetInstanceModsDir(branch, version), "manifest.json")
}

// LoadManifest loads the mod manifest (legacy)
func LoadManifest() (*ModManifest, error) {
	path := GetModManifestPath()
	return loadManifestFromPath(path)
}

// LoadInstanceManifest loads the mod manifest for a specific instance
func LoadInstanceManifest(branch string, version int) (*ModManifest, error) {
	path := GetInstanceModManifestPath(branch, version)
	return loadManifestFromPath(path)
}

// loadManifestFromPath loads a manifest from a specific path
func loadManifestFromPath(path string) (*ModManifest, error) {
	data, err := os.ReadFile(path)
	if err != nil {
		if os.IsNotExist(err) {
			return &ModManifest{Mods: []Mod{}, Version: "1.0"}, nil
		}
		return nil, err
	}

	var manifest ModManifest
	if err := json.Unmarshal(data, &manifest); err != nil {
		return nil, err
	}

	return &manifest, nil
}

// SaveManifest saves the mod manifest (legacy)
func SaveManifest(manifest *ModManifest) error {
	path := GetModManifestPath()
	return saveManifestToPath(manifest, path)
}

// SaveInstanceManifest saves the mod manifest for a specific instance
func SaveInstanceManifest(manifest *ModManifest, branch string, version int) error {
	path := GetInstanceModManifestPath(branch, version)
	return saveManifestToPath(manifest, path)
}

// saveManifestToPath saves a manifest to a specific path
func saveManifestToPath(manifest *ModManifest, path string) error {
	if err := os.MkdirAll(filepath.Dir(path), 0755); err != nil {
		return err
	}

	data, err := json.MarshalIndent(manifest, "", "  ")
	if err != nil {
		return err
	}

	return os.WriteFile(path, data, 0644)
}

// GetInstalledMods returns all installed mods (legacy)
func GetInstalledMods() ([]Mod, error) {
	manifest, err := LoadManifest()
	if err != nil {
		return nil, err
	}
	return manifest.Mods, nil
}

// GetInstanceInstalledMods returns all installed mods for a specific instance
func GetInstanceInstalledMods(branch string, version int) ([]Mod, error) {
	manifest, err := LoadInstanceManifest(branch, version)
	if err != nil {
		return nil, err
	}
	return manifest.Mods, nil
}

// AddMod adds a mod to the manifest (legacy)
func AddMod(mod Mod) error {
	manifest, err := LoadManifest()
	if err != nil {
		return err
	}

	// Check if already exists
	for i, m := range manifest.Mods {
		if m.ID == mod.ID {
			manifest.Mods[i] = mod
			return SaveManifest(manifest)
		}
	}

	manifest.Mods = append(manifest.Mods, mod)
	return SaveManifest(manifest)
}

// AddInstanceMod adds a mod to an instance's manifest
func AddInstanceMod(mod Mod, branch string, version int) error {
	manifest, err := LoadInstanceManifest(branch, version)
	if err != nil {
		return err
	}

	// Check if already exists
	for i, m := range manifest.Mods {
		if m.ID == mod.ID {
			manifest.Mods[i] = mod
			return SaveInstanceManifest(manifest, branch, version)
		}
	}

	manifest.Mods = append(manifest.Mods, mod)
	return SaveInstanceManifest(manifest, branch, version)
}

// RemoveMod removes a mod from manifest and deletes files (legacy)
func RemoveMod(modID string) error {
	manifest, err := LoadManifest()
	if err != nil {
		return err
	}

	var newMods []Mod
	var modToRemove *Mod
	for _, m := range manifest.Mods {
		if m.ID == modID {
			modToRemove = &m
		} else {
			newMods = append(newMods, m)
		}
	}

	if modToRemove == nil {
		return fmt.Errorf("mod not found: %s", modID)
	}

	// Delete mod file
	if modToRemove.FilePath != "" {
		if err := os.Remove(modToRemove.FilePath); err != nil && !os.IsNotExist(err) {
			return err
		}
	}

	manifest.Mods = newMods
	return SaveManifest(manifest)
}

// RemoveInstanceMod removes a mod from an instance's manifest and deletes files
func RemoveInstanceMod(modID string, branch string, version int) error {
	manifest, err := LoadInstanceManifest(branch, version)
	if err != nil {
		return err
	}

	var newMods []Mod
	var modToRemove *Mod
	for _, m := range manifest.Mods {
		if m.ID == modID {
			modCopy := m
			modToRemove = &modCopy
		} else {
			newMods = append(newMods, m)
		}
	}

	if modToRemove == nil {
		return fmt.Errorf("mod not found: %s", modID)
	}

	// Delete mod file
	if modToRemove.FilePath != "" {
		if err := os.Remove(modToRemove.FilePath); err != nil && !os.IsNotExist(err) {
			return err
		}
	}

	manifest.Mods = newMods
	return SaveInstanceManifest(manifest, branch, version)
}

// ToggleMod enables or disables a mod (legacy)
func ToggleMod(modID string, enabled bool) error {
	manifest, err := LoadManifest()
	if err != nil {
		return err
	}

	for i, m := range manifest.Mods {
		if m.ID == modID {
			manifest.Mods[i].Enabled = enabled
			
			// Rename file to enable/disable
			oldPath := m.FilePath
			newPath := oldPath
			
			if enabled && filepath.Ext(oldPath) == ".disabled" {
				newPath = oldPath[:len(oldPath)-9] // Remove .disabled
			} else if !enabled && filepath.Ext(oldPath) != ".disabled" {
				newPath = oldPath + ".disabled"
			}
			
			if oldPath != newPath {
				if err := os.Rename(oldPath, newPath); err != nil {
					return err
				}
				manifest.Mods[i].FilePath = newPath
			}
			
			return SaveManifest(manifest)
		}
	}

	return fmt.Errorf("mod not found: %s", modID)
}

// ToggleInstanceMod enables or disables a mod in an instance
func ToggleInstanceMod(modID string, enabled bool, branch string, version int) error {
	manifest, err := LoadInstanceManifest(branch, version)
	if err != nil {
		return err
	}

	for i, m := range manifest.Mods {
		if m.ID == modID {
			manifest.Mods[i].Enabled = enabled
			
			// Rename file to enable/disable
			oldPath := m.FilePath
			newPath := oldPath
			
			if enabled && filepath.Ext(oldPath) == ".disabled" {
				newPath = oldPath[:len(oldPath)-9] // Remove .disabled
			} else if !enabled && filepath.Ext(oldPath) != ".disabled" {
				newPath = oldPath + ".disabled"
			}
			
			if oldPath != newPath {
				if err := os.Rename(oldPath, newPath); err != nil {
					return err
				}
				manifest.Mods[i].FilePath = newPath
			}
			
			return SaveInstanceManifest(manifest, branch, version)
		}
	}

	return fmt.Errorf("mod not found: %s", modID)
}
