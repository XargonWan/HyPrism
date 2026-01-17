//go:build !windows

package updater

import (
	"fmt"
	"os"
	"os/exec"
	"path/filepath"
	"runtime"
)

// Apply applies a launcher update on Unix systems and restarts the app
func Apply(tmp string) error {
	exe, err := os.Executable()
	if err != nil {
		return fmt.Errorf("failed to get executable path: %w", err)
	}

	// Create a shell script to replace the binary and restart
	scriptPath := filepath.Join(os.TempDir(), "hyprism-update.sh")
	
	var script string
	if runtime.GOOS == "darwin" {
		// For macOS .app bundles - find the actual app bundle path
		appBundlePath := exe
		// Navigate up from Contents/MacOS/executable to the .app bundle
		for i := 0; i < 3; i++ {
			appBundlePath = filepath.Dir(appBundlePath)
		}
		if filepath.Ext(appBundlePath) != ".app" {
			// Not in a bundle, use regular executable path
			appBundlePath = exe
		}
		
		script = fmt.Sprintf(`#!/bin/bash
sleep 1
rm -rf "%s.old" 2>/dev/null
mv "%s" "%s.old" 2>/dev/null
cp "%s" "%s"
chmod +x "%s"
rm -rf "%s.old"
rm -f "%s"
# Restart the application
open "%s"
rm -f "%s"
`, exe, exe, exe, tmp, exe, exe, exe, tmp, appBundlePath, scriptPath)
	} else {
		// Linux
		script = fmt.Sprintf(`#!/bin/bash
sleep 1
mv "%s" "%s.old" 2>/dev/null
cp "%s" "%s"
chmod +x "%s"
rm -f "%s.old"
rm -f "%s"
# Restart the application
"%s" &
rm -f "%s"
`, exe, exe, tmp, exe, exe, exe, tmp, exe, scriptPath)
	}

	if err := os.WriteFile(scriptPath, []byte(script), 0755); err != nil {
		return fmt.Errorf("failed to create update script: %w", err)
	}

	cmd := exec.Command("/bin/bash", scriptPath)
	cmd.Stdout = os.Stdout
	cmd.Stderr = os.Stderr

	if err := cmd.Start(); err != nil {
		return fmt.Errorf("failed to start update script: %w", err)
	}

	os.Exit(0)
	return nil
}
