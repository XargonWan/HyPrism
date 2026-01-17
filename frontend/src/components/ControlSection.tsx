import React from 'react';
import { motion } from 'framer-motion';
import { FolderOpen, Trash, Play, Package, Square, Download } from 'lucide-react';

interface ControlSectionProps {
  onPlay: () => void;
  onExit?: () => void;
  onInstallVersion?: (version: number) => void;
  isDownloading: boolean;
  isGameRunning: boolean;
  progress: number;
  status: string;
  speed: string;
  downloaded: number;
  total: number;
  currentFile: string;
  currentVersion: string;
  latestVersion: number;
  actions: {
    openFolder: () => void;
    showDelete: () => void;
    showModManager: () => void;
  };
}

const NavBtn: React.FC<{ onClick?: () => void; icon: React.ReactNode; tooltip?: string }> = ({ onClick, icon, tooltip }) => (
  <motion.button
    whileHover={{ scale: 1.05, backgroundColor: 'rgba(255, 168, 69, 0.1)' }}
    whileTap={{ scale: 0.95 }}
    onClick={onClick}
    className="w-12 h-12 rounded-xl glass border border-white/5 flex items-center justify-center text-white/60 hover:text-[#FFA845] transition-colors relative group"
    title={tooltip}
  >
    {icon}
    {tooltip && (
      <span className="absolute -top-10 left-1/2 -translate-x-1/2 px-2 py-1 text-xs bg-black/90 text-white rounded opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap pointer-events-none">
        {tooltip}
      </span>
    )}
  </motion.button>
);

const formatBytes = (bytes: number): string => {
  if (bytes === 0) return '0 B';
  const k = 1024;
  const sizes = ['B', 'KB', 'MB', 'GB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return `${parseFloat((bytes / Math.pow(k, i)).toFixed(1))} ${sizes[i]}`;
};

export const ControlSection: React.FC<ControlSectionProps> = ({
  onPlay,
  onExit,
  onInstallVersion,
  isDownloading,
  isGameRunning,
  progress,
  status,
  speed,
  downloaded,
  total,
  currentFile,
  currentVersion,
  latestVersion,
  actions
}) => {
  // Parse current version number
  const currentVersionNum = parseInt(currentVersion.match(/build (\d+)/)?.[1] || '0');
  const hasUpdate = currentVersionNum > 0 && latestVersion > currentVersionNum;

  return (
    <motion.div 
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      className="flex gap-4"
    >
      {/* Left side - Navigation buttons */}
      <div className="flex flex-col gap-3">
        <div className="flex gap-3">
          <NavBtn onClick={actions.showModManager} icon={<Package size={20} />} tooltip="Mod Manager" />
          <NavBtn onClick={actions.openFolder} icon={<FolderOpen size={20} />} tooltip="Open Folder" />
          <NavBtn onClick={actions.showDelete} icon={<Trash size={20} />} tooltip="Delete Game" />
        </div>
        
        {/* Play/Exit button */}
        <div className="flex gap-3">
          {isGameRunning ? (
            <motion.button
              whileTap={{ scale: 0.98 }}
              whileHover={{ scale: 1.02 }}
              onClick={onExit}
              className="flex-1 h-24 rounded-2xl font-black text-4xl tracking-tight flex items-center justify-center gap-4 transition-all duration-300 bg-gradient-to-r from-red-600 to-red-500 text-white hover:shadow-lg hover:shadow-red-500/25 cursor-pointer"
            >
              <Square size={32} fill="currentColor" />
              <span>EXIT</span>
            </motion.button>
          ) : (
            <motion.button
              whileTap={isDownloading ? {} : { scale: 0.98 }}
              whileHover={isDownloading ? {} : { scale: 1.02 }}
              onClick={onPlay}
              disabled={isDownloading}
              className={`
                flex-1 h-24 rounded-2xl font-black text-4xl tracking-tight
                flex items-center justify-center gap-4
                transition-all duration-300
                ${isDownloading 
                  ? 'bg-[#151515] text-white/50 cursor-not-allowed' 
                  : 'bg-gradient-to-r from-[#FFA845] to-[#FF6B35] text-white hover:shadow-lg hover:shadow-[#FFA845]/25 cursor-pointer'
                }
              `}
            >
              {isDownloading ? (
                <span className="text-2xl font-bold">DOWNLOADING...</span>
              ) : (
                <>
                  <Play size={32} fill="currentColor" />
                  <span>PLAY</span>
                </>
              )}
            </motion.button>
          )}
        </div>
      </div>

      {/* Right side - Version & Status */}
      <div className="flex-1 glass rounded-2xl border border-white/5 p-5 flex flex-col justify-between min-w-[280px]">
        <div>
          <div className="flex items-center justify-between mb-3">
            <h3 className="text-lg font-bold text-white">
              {isGameRunning ? 'Game Running' : isDownloading ? 'Downloading Hytale' : 'Hytale'}
            </h3>
          </div>
          
          {/* Current Version & Update */}
          <div className="flex items-center gap-3 mb-2">
            <span className="text-sm text-gray-400">
              Installed: <span className="text-white font-medium">{currentVersion || 'Not installed'}</span>
            </span>
            {hasUpdate && !isDownloading && (
              <button
                onClick={() => onInstallVersion?.(latestVersion)}
                className="flex items-center gap-1 px-2 py-1 rounded-md bg-[#FFA845]/20 text-[#FFA845] text-xs font-medium hover:bg-[#FFA845]/30 transition-colors"
              >
                <Download size={12} />
                Update to build {latestVersion}
              </button>
            )}
          </div>
          
          <p className="text-sm text-gray-500">{status}</p>
        </div>

        {isDownloading && (
          <div className="mt-4">
            {/* Progress bar */}
            <div className="relative h-2 bg-white/5 rounded-full overflow-hidden">
              <motion.div
                initial={{ width: 0 }}
                animate={{ width: `${Math.min(progress, 100)}%` }}
                transition={{ duration: 0.3 }}
                className="absolute inset-y-0 left-0 bg-gradient-to-r from-[#FFA845] to-[#FF6B35] rounded-full"
              />
            </div>

            {/* Stats */}
            <div className="flex justify-between items-center mt-3 text-xs text-gray-400">
              <div className="flex gap-4">
                <span>{formatBytes(downloaded)} / {formatBytes(total)}</span>
                {speed && <span className="text-[#FFA845]">{speed}</span>}
              </div>
              <span className="font-bold text-white">{Math.round(progress)}%</span>
            </div>

            {currentFile && (
              <p className="text-xs text-gray-500 mt-2 truncate">
                {currentFile}
              </p>
            )}
          </div>
        )}
      </div>
    </motion.div>
  );
};
