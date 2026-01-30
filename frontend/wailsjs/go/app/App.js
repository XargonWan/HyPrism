// Photino Bridge - Replaces Wails Go bindings
// This file provides Wails-compatible API calls using Photino's messaging

// RPC system for calling C# backend
const pendingCalls = new Map();
let callId = 0;

// Register callback for receiving messages from C# (Photino uses this pattern)
if (!window._photinoListenerAdded) {
    window._photinoListenerAdded = true;
    
    // Photino sends messages via window.external.receiveMessage callback
    window.external.receiveMessage((message) => {
        try {
            const data = typeof message === 'string' ? JSON.parse(message) : message;
            
            // Handle progress events
            if (data.type === 'event' && data.eventName) {
                window.dispatchEvent(new CustomEvent(data.eventName, { detail: data.data }));
                return;
            }
            
            // Handle RPC responses
            if (data.Id && pendingCalls.has(data.Id)) {
                const { resolve, reject } = pendingCalls.get(data.Id);
                pendingCalls.delete(data.Id);
                if (data.Error) {
                    reject(new Error(data.Error));
                } else {
                    resolve(data.Result);
                }
            }
        } catch (e) {
            console.error('Error parsing message:', e);
        }
    });
}

function callBackend(method, ...args) {
    return new Promise((resolve, reject) => {
        const id = `call_${++callId}`;
        pendingCalls.set(id, { resolve, reject });
        
        const message = JSON.stringify({
            Id: id,
            Method: method,
            Args: args
        });
        
        if (window.external?.sendMessage) {
            window.external.sendMessage(message);
        } else {
            console.warn('No native bridge available, returning mock data');
            // Return mock data for development
            setTimeout(() => {
                resolve(getMockResult(method));
            }, 100);
        }
    });
}

// Mock data for development mode
function getMockResult(method) {
    const mocks = {
        'GetNick': 'Player',
        'GetVersionType': 'prototype',
        'GetVersionList': [1, 2, 3],
        'GetInstalledVersionsForBranch': [],
        'IsVersionInstalled': false,
        'CheckLatestNeedsUpdate': false,
        'GetLauncherVersion': '1.0.0-dev',
        'IsGameRunning': false,
        'GetOnlineMode': true,
        'GetMusicEnabled': true,
        'GetNews': [],
        'GetInstanceInstalledMods': [],
        'GetModCategories': [],
        'SearchMods': { mods: [], totalCount: 0 },
        'GetModFiles': [],
        'CheckInstanceModUpdates': [],
    };
    return mocks[method] ?? null;
}

// App functions
export function CheckInstanceModUpdates(branch, version) {
    return callBackend('CheckInstanceModUpdates', branch, version);
}

export function CheckLatestNeedsUpdate(branch) {
    return callBackend('CheckLatestNeedsUpdate', branch);
}

export function GetPendingUpdateInfo(branch) {
    return callBackend('GetPendingUpdateInfo', branch);
}

export function CopyUserData(branch, fromVersion, toVersion) {
    return callBackend('CopyUserData', branch, fromVersion, toVersion);
}

export function CheckModUpdates() {
    return callBackend('CheckModUpdates');
}

export function CheckUpdate() {
    return callBackend('CheckUpdate');
}

export function CheckVersionAvailability() {
    return callBackend('CheckVersionAvailability');
}

export function CancelDownload() {
    return callBackend('CancelDownload');
}

export function DeleteGame(branch, version) {
    return callBackend('DeleteGame', branch, version);
}

export function DownloadAndLaunch(version) {
    return callBackend('DownloadAndLaunch', version);
}

export function DownloadVersion(branch, version) {
    return callBackend('DownloadVersion', branch, version);
}

export function ExitGame() {
    return callBackend('ExitGame');
}

export function GetAuthDomain() {
    return callBackend('GetAuthDomain');
}

export function GetAutoUpdateLatest() {
    return callBackend('GetAutoUpdateLatest');
}

export function GetAvailableVersions() {
    return callBackend('GetAvailableVersions');
}

export function GetConfig() {
    return callBackend('GetConfig');
}

export function GetCrashReports() {
    return callBackend('GetCrashReports');
}

export function GetCurrentVersion() {
    return callBackend('GetCurrentVersion');
}

export function GetCustomInstanceDir() {
    return callBackend('GetCustomInstanceDir');
}

export function GetGameLogs() {
    return callBackend('GetGameLogs');
}

export function GetRecentLogs(count) {
    return callBackend('GetRecentLogs', count || 10);
}

export function GetGamePath() {
    return callBackend('GetGamePath');
}

export function HasAssetsZip(branch, version) {
    return callBackend('HasAssetsZip', branch, version);
}

export function GetAssetsZipPath(branch, version) {
    return callBackend('GetAssetsZipPath', branch, version);
}

export function GetInstalledMods() {
    return callBackend('GetInstalledMods');
}

export function GetInstalledVersions() {
    return callBackend('GetInstalledVersions');
}

export function GetInstalledVersionsForBranch(branch) {
    return callBackend('GetInstalledVersionsForBranch', branch);
}

export function GetInstanceInstalledMods(branch, version) {
    return callBackend('GetInstanceInstalledMods', branch, version);
}

export function GetLauncherVersion() {
    return callBackend('GetLauncherVersion');
}

export function GetLogs() {
    return callBackend('GetLogs');
}

export function GetModCategories() {
    return callBackend('GetModCategories');
}

export function GetModDetails(modId) {
    return callBackend('GetModDetails', modId);
}

export function GetModFiles(modId, page = 0, pageSize = 50) {
    return callBackend('GetModFiles', modId?.toString() || '', page, pageSize);
}

export function GetMusicEnabled() {
    return callBackend('GetMusicEnabled');
}

export function GetNews(count) {
    return callBackend('GetNews', count);
}

export function GetNick() {
    return callBackend('GetNick');
}

export function GetUUID() {
    return callBackend('GetUUID');
}

export function GetAvatarPreview() {
    return callBackend('GetAvatarPreview');
}

export function SetUUID(uuid) {
    return callBackend('SetUUID', uuid);
}

export function GetOnlineMode() {
    return callBackend('GetOnlineMode');
}

export function GetPlatformInfo() {
    return callBackend('GetPlatformInfo');
}

export function GetSelectedVersion() {
    return callBackend('GetSelectedVersion');
}

export function GetVersionList(branch) {
    return callBackend('GetVersionList', branch);
}

export function GetVersionType() {
    return callBackend('GetVersionType');
}

export function GetVersions() {
    return callBackend('GetVersions');
}

export function InstallMod(modId) {
    return callBackend('InstallMod', modId);
}

export function InstallModFile(modId, fileId) {
    return callBackend('InstallModFile', modId, fileId);
}

export function InstallModFileToInstance(modId, fileId, branch, version) {
    return callBackend('InstallModFileToInstance', modId, fileId, branch, version);
}

export function InstallModToInstance(modId, slug, version) {
    return callBackend('InstallModToInstance', modId, slug, version);
}

export function IsGameInstalled() {
    return callBackend('IsGameInstalled');
}

export function IsGameRunning() {
    return callBackend('IsGameRunning');
}

export function IsVersionInstalled(branch, version) {
    return callBackend('IsVersionInstalled', branch, version);
}

export function OpenFolder() {
    return callBackend('OpenFolder');
}

export function OpenGameFolder() {
    return callBackend('OpenGameFolder');
}

export function OpenInstanceModsFolder(branch, version) {
    return callBackend('OpenInstanceModsFolder', branch, version);
}

export function OpenInstanceFolder(branch, version) {
    return callBackend('OpenInstanceFolder', branch, version);
}

export function OpenModsFolder() {
    return callBackend('OpenModsFolder');
}

export function QuickLaunch() {
    return callBackend('QuickLaunch');
}

export function RepairInstallation() {
    return callBackend('RepairInstallation');
}

export function RunDiagnostics() {
    return callBackend('RunDiagnostics');
}

export function SaveConfig() {
    return callBackend('SaveConfig');
}

export function SaveDiagnosticReport() {
    return callBackend('SaveDiagnosticReport');
}

export function SearchMods(query, page, pageSize, categories, sortField, sortOrder) {
    return callBackend('SearchMods', query, page, pageSize, categories || [], sortField || 2, sortOrder || 2);
}

export function SelectInstanceDirectory() {
    return callBackend('SelectInstanceDirectory');
}

export function BrowseFolder(initialPath) {
    return callBackend('BrowseFolder', initialPath || null);
}

export function SetInstanceDirectory(path) {
    return callBackend('SetInstanceDirectory', path);
}

export function SetAuthDomain(domain) {
    return callBackend('SetAuthDomain', domain);
}

// Skin configuration
export function GetSkinConfig() {
    return callBackend('GetSkinConfig');
}

export function SaveSkinConfig(config) {
    return callBackend('SaveSkinConfig', config);
}

export function GetInstanceSkinConfig(branch, version) {
    return callBackend('GetInstanceSkinConfig', branch, version);
}

export function ApplySkinToInstance(branch, version) {
    return callBackend('ApplySkinToInstance', branch, version);
}

export function SetAutoUpdateLatest(enabled) {
    return callBackend('SetAutoUpdateLatest', enabled);
}

export function SetCustomInstanceDir(dir) {
    return callBackend('SetCustomInstanceDir', dir);
}

export function SetMusicEnabled(enabled) {
    return callBackend('SetMusicEnabled', enabled);
}

export function SetNick(nick) {
    return callBackend('SetNick', nick);
}

export function SetOnlineMode(enabled) {
    return callBackend('SetOnlineMode', enabled);
}

export function SetSelectedVersion(version) {
    return callBackend('SetSelectedVersion', version);
}

export function SetVersionType(type) {
    return callBackend('SetVersionType', type);
}

export function SwitchVersion(version) {
    return callBackend('SwitchVersion', version);
}

export function ToggleInstanceMod(modId, enabled, branch, version) {
    return callBackend('ToggleInstanceMod', modId, enabled, branch, version);
}

export function ToggleMod(modId, enabled) {
    return callBackend('ToggleMod', modId, enabled);
}

export function UninstallInstanceMod(modId, branch, version) {
    return callBackend('UninstallInstanceMod', modId, branch, version);
}

export function UninstallMod(modId) {
    return callBackend('UninstallMod', modId);
}

export function Update() {
    return callBackend('Update');
}

export function GetLauncherBranch() {
    return callBackend('GetLauncherBranch');
}

export function SetLauncherBranch(branch) {
    return callBackend('SetLauncherBranch', branch);
}

export function CheckRosettaStatus() {
    return callBackend('CheckRosettaStatus');
}

export function GetCloseAfterLaunch() {
    return callBackend('GetCloseAfterLaunch');
}

export function SetCloseAfterLaunch(enabled) {
    return callBackend('SetCloseAfterLaunch', enabled);
}

export function GetShowDiscordAnnouncements() {
    return callBackend('GetShowDiscordAnnouncements');
}

export function SetShowDiscordAnnouncements(enabled) {
    return callBackend('SetShowDiscordAnnouncements', enabled);
}

export function DismissAnnouncement(announcementId) {
    return callBackend('DismissAnnouncement', announcementId);
}

export function ReactToAnnouncement(messageId, emoji) {
    return callBackend('ReactToAnnouncement', messageId, emoji);
}

export function GetDiscordAnnouncement() {
    return callBackend('GetDiscordAnnouncement');
}
export function OpenLauncherFolder() {
    return callBackend('OpenLauncherFolder');
}

export function DeleteLauncherData() {
    return callBackend('DeleteLauncherData');
}

export function GetLauncherFolderPath() {
    return callBackend('GetLauncherFolderPath');
}

export function GetTestAnnouncement() {
    return callBackend('GetTestAnnouncement');
}

// News settings
export function GetDisableNews() {
    return callBackend('GetDisableNews');
}

export function SetDisableNews(disabled) {
    return callBackend('SetDisableNews', disabled);
}

// Background settings
export function GetBackgroundMode() {
    return callBackend('GetBackgroundMode');
}

export function SetBackgroundMode(mode) {
    return callBackend('SetBackgroundMode', mode);
}

export function GetAvailableBackgrounds() {
    return callBackend('GetAvailableBackgrounds');
}

// Accent color settings
export function GetAccentColor() {
    return callBackend('GetAccentColor');
}

export function SetAccentColor(color) {
    return callBackend('SetAccentColor', color);
}

// Launcher Data Directory
export function GetLauncherDataDirectory() {
    return callBackend('GetLauncherDataDirectory');
}

export function SetLauncherDataDirectory(path) {
    return callBackend('SetLauncherDataDirectory', path);
}

export function GetDisableHardwareAcceleration() {
    return callBackend('GetDisableHardwareAcceleration');
}

export function SetDisableHardwareAcceleration(disabled) {
    return callBackend('SetDisableHardwareAcceleration', disabled);
}

export function WindowClose() {
    return callBackend('WindowClose');
}