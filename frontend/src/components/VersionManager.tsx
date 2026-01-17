import React, { useState, useEffect } from 'react';
import { motion } from 'framer-motion';
import { X, Info, RefreshCw } from 'lucide-react';

interface VersionManagerProps {
  onClose: () => void;
  getCurrentVersionType: () => Promise<string>;
  setVersionType: (versionType: string) => Promise<void>;
  checkVersion: () => Promise<{ available: boolean; version: number }>;
}

export const VersionManager: React.FC<VersionManagerProps> = ({
  onClose,
  getCurrentVersionType,
  setVersionType,
  checkVersion
}) => {
  const [currentVersionType, setCurrentVersionType] = useState<string>('release');
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [selectedType, setSelectedType] = useState<string>('release');
  const [versionInfo, setVersionInfo] = useState<{ available: boolean; version: number } | null>(null);
  const [isCheckingVersion, setIsCheckingVersion] = useState(false);

  useEffect(() => {
    loadCurrentVersionType();
  }, []);

  const loadCurrentVersionType = async () => {
    setIsLoading(true);
    try {
      const vType = await getCurrentVersionType();
      const normalized = vType === 'pre-release' || vType === 'prerelease' ? 'pre-release' : 'release';
      setCurrentVersionType(normalized);
      setSelectedType(normalized);
    } catch (err) {
      console.error('Failed to get version type:', err);
    }
    setIsLoading(false);
  };

  const handleCheckVersion = async () => {
    setIsCheckingVersion(true);
    try {
      const info = await checkVersion();
      setVersionInfo(info);
    } catch (err) {
      console.error('Failed to check version:', err);
    }
    setIsCheckingVersion(false);
  };

  const handleSave = async () => {
    if (selectedType === currentVersionType) {
      onClose();
      return;
    }

    setIsSaving(true);
    try {
      await setVersionType(selectedType);
      setCurrentVersionType(selectedType);
      onClose();
    } catch (err) {
      console.error('Failed to set version type:', err);
    }
    setIsSaving(false);
  };

  const versionTypes = [
    {
      id: 'release',
      name: 'Release',
      description: 'Stable release versions recommended for most users.',
      badge: 'Stable'
    },
    {
      id: 'pre-release',
      name: 'Pre-Release',
      description: 'Early access versions with new features. May contain bugs.',
      badge: 'Beta'
    }
  ];

  return (
    <motion.div
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0 }}
      className="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4"
      onClick={onClose}
    >
      <motion.div
        initial={{ opacity: 0, scale: 0.95, y: 20 }}
        animate={{ opacity: 1, scale: 1, y: 0 }}
        exit={{ opacity: 0, scale: 0.95, y: 20 }}
        onClick={(e) => e.stopPropagation()}
        className="bg-[#1a1a1a] rounded-2xl w-full max-w-md overflow-hidden flex flex-col border border-white/10"
      >
        {/* Header */}
        <div className="p-6 border-b border-white/10 flex items-center justify-between">
          <h2 className="text-xl font-bold text-white">Version Manager</h2>
          <button
            onClick={onClose}
            className="p-2 hover:bg-white/10 rounded-lg transition-colors"
          >
            <X size={20} className="text-gray-400" />
          </button>
        </div>

        {/* Content */}
        <div className="p-6 space-y-4">
          {isLoading ? (
            <div className="flex items-center justify-center py-8">
              <RefreshCw size={24} className="text-[#FFA845] animate-spin" />
            </div>
          ) : (
            <>
              <p className="text-sm text-gray-400">
                Select which version channel you want to use. Switching channels will download the corresponding game version.
              </p>

              <div className="space-y-3">
                {versionTypes.map((type) => (
                  <button
                    key={type.id}
                    onClick={() => setSelectedType(type.id)}
                    className={`w-full p-4 rounded-xl border text-left transition-all ${
                      selectedType === type.id
                        ? 'border-[#FFA845] bg-[#FFA845]/10'
                        : 'border-white/10 bg-white/5 hover:bg-white/10'
                    }`}
                  >
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-3">
                        <div className={`w-5 h-5 rounded-full border-2 flex items-center justify-center ${
                          selectedType === type.id
                            ? 'border-[#FFA845]'
                            : 'border-white/30'
                        }`}>
                          {selectedType === type.id && (
                            <div className="w-2.5 h-2.5 rounded-full bg-[#FFA845]" />
                          )}
                        </div>
                        <div>
                          <div className="flex items-center gap-2">
                            <span className="font-bold text-white">{type.name}</span>
                            <span className={`text-xs px-2 py-0.5 rounded ${
                              type.id === 'release' 
                                ? 'bg-green-500/20 text-green-400' 
                                : 'bg-yellow-500/20 text-yellow-400'
                            }`}>
                              {type.badge}
                            </span>
                            {currentVersionType === type.id && (
                              <span className="text-xs px-2 py-0.5 rounded bg-white/10 text-gray-400">
                                Current
                              </span>
                            )}
                          </div>
                          <p className="text-xs text-gray-400 mt-1">{type.description}</p>
                        </div>
                      </div>
                    </div>
                  </button>
                ))}
              </div>

              {/* Version check info */}
              {versionInfo && (
                <div className="p-3 rounded-lg bg-white/5 border border-white/10">
                  <div className="flex items-center gap-2 text-sm">
                    <Info size={16} className="text-[#FFA845]" />
                    <span className="text-gray-400">
                      {versionInfo.available 
                        ? `Version ${versionInfo.version} available`
                        : 'No version available'
                      }
                    </span>
                  </div>
                </div>
              )}

              <button
                onClick={handleCheckVersion}
                disabled={isCheckingVersion}
                className="w-full py-2 rounded-lg bg-white/5 hover:bg-white/10 text-gray-400 text-sm font-medium transition-colors flex items-center justify-center gap-2"
              >
                <RefreshCw size={16} className={isCheckingVersion ? 'animate-spin' : ''} />
                {isCheckingVersion ? 'Checking...' : 'Check Available Version'}
              </button>
            </>
          )}
        </div>

        {/* Footer */}
        <div className="p-6 border-t border-white/10 flex gap-3">
          <button
            onClick={onClose}
            className="flex-1 py-3 rounded-xl font-medium bg-white/5 hover:bg-white/10 text-gray-400 transition-colors"
          >
            Cancel
          </button>
          <button
            onClick={handleSave}
            disabled={isSaving || selectedType === currentVersionType}
            className={`flex-1 py-3 rounded-xl font-bold transition-colors ${
              selectedType === currentVersionType
                ? 'bg-white/5 text-gray-500 cursor-not-allowed'
                : 'bg-gradient-to-r from-[#FFA845] to-[#FF6B35] text-black hover:shadow-lg'
            }`}
          >
            {isSaving ? (
              <RefreshCw size={20} className="animate-spin mx-auto" />
            ) : selectedType === currentVersionType ? (
              'No Changes'
            ) : (
              'Apply Changes'
            )}
          </button>
        </div>
      </motion.div>
    </motion.div>
  );
};
