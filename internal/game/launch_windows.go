//go:build windows

package game

import "syscall"

// Windows constants for process creation
const (
	CREATE_NEW_PROCESS_GROUP = 0x00000200
	DETACHED_PROCESS         = 0x00000008
	CREATE_NO_WINDOW         = 0x08000000
)

// getWindowsSysProcAttr returns Windows-specific process attributes
// This hides the console window and detaches the process
func getWindowsSysProcAttr() *syscall.SysProcAttr {
	return &syscall.SysProcAttr{
		CreationFlags: CREATE_NEW_PROCESS_GROUP | DETACHED_PROCESS | CREATE_NO_WINDOW,
		HideWindow:    true,
	}
}
