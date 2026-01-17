package config

// Config represents the launcher configuration
type Config struct {
	Version         string `toml:"version" json:"version"`
	Nick            string `toml:"nick" json:"nick"`
	MusicEnabled    bool   `toml:"music_enabled" json:"musicEnabled"`
	VersionType     string `toml:"version_type" json:"versionType"`
	SelectedVersion int    `toml:"selected_version" json:"selectedVersion"`
}

// Default returns the default configuration
func Default() *Config {
	return &Config{
		Version:         "1.0.0",
		Nick:            "HyPrism",
		MusicEnabled:    true,
		VersionType:     "release",
		SelectedVersion: 0, // 0 means use latest
	}
}
