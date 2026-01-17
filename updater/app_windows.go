//go:build windows

package updater

import (
	"HyPrism/internal/util"
	"fmt"
	"os"
	"os/exec"
	"path/filepath"
	"syscall"
)

// Apply applies a launcher update on Windows
func Apply(tmp string) error {
	exe, err := os.Executable()
	if err != nil {
		return fmt.Errorf("failed to get executable path: %w", err)
	}

	// Create a batch script to replace the executable
	scriptPath := filepath.Join(os.TempDir(), "hyprism-update.bat")
	script := fmt.Sprintf(`@echo off
timeout /t 1 /nobreak >nul
del /f /q "%s.old" 2>nul
ren "%s" "%s.old" 2>nul
copy /y "%s" "%s" >nul
del /f /q "%s.old" 2>nul
del /f /q "%s" 2>nul
exit
`,
		exe, exe, filepath.Base(exe), tmp, exe, exe, tmp)

	if err := os.WriteFile(scriptPath, []byte(script), 0755); err != nil {
		return fmt.Errorf("failed to create update script: %w", err)
	}

	cmd := exec.Command("cmd.exe", "/C", scriptPath)
	cmd.SysProcAttr = &syscall.SysProcAttr{
		CreationFlags: syscall.CREATE_NO_WINDOW,
	}
	util.HideConsoleWindow(cmd)

	if err := cmd.Start(); err != nil {
		return fmt.Errorf("failed to start update script: %w", err)
	}

	os.Exit(0)
	return nil
}
