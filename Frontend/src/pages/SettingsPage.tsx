import React, { Suspense, lazy } from 'react';
import { motion } from 'framer-motion';
import { Settings } from 'lucide-react';
import { useTranslation } from 'react-i18next';

const SettingsModal = lazy(() => import('../components/SettingsModal').then(m => ({ default: m.SettingsModal })));

const pageVariants = {
  initial: { opacity: 0, y: 12 },
  animate: { opacity: 1, y: 0 },
  exit: { opacity: 0, y: -12 },
};

interface SettingsPageProps {
  launcherBranch: string;
  onLauncherBranchChange: (branch: string) => void;
  rosettaWarning?: { message: string; command: string; tutorialUrl?: string } | null;
  onBackgroundModeChange?: (mode: string) => void;
  onAccentColorChange?: (color: string) => void;
  onInstanceDeleted?: () => void;
  onNavigateToMods?: () => void;
  onAuthSettingsChange?: () => void;
  isGameRunning?: boolean;
  onMovingDataChange?: (isMoving: boolean) => void;
}

export const SettingsPage: React.FC<SettingsPageProps> = (props) => {
  const { t } = useTranslation();

  return (
    <motion.div
      variants={pageVariants}
      initial="initial"
      animate="animate"
      exit="exit"
      transition={{ duration: 0.3, ease: 'easeOut' }}
      className="h-full flex flex-col px-4 pt-6 pb-28"
    >
      {/* Page Header */}
      <div className="flex items-center justify-between mb-4 flex-shrink-0">
        <div className="flex items-center gap-3">
          <Settings size={22} className="text-white/80" />
          <h1 className="text-xl font-bold text-white">{t('settings.title')}</h1>
        </div>
      </div>

      {/* Content */}
      <div className="flex-1 min-h-0">
        <Suspense fallback={
          <div className="flex items-center justify-center h-full">
            <div className="w-8 h-8 border-2 border-white/20 border-t-white rounded-full animate-spin" />
          </div>
        }>
          <SettingsModal
            onClose={() => {}}
            launcherBranch={props.launcherBranch}
            onLauncherBranchChange={props.onLauncherBranchChange}
            onShowModManager={props.onNavigateToMods}
            rosettaWarning={props.rosettaWarning}
            onBackgroundModeChange={props.onBackgroundModeChange}
            onAccentColorChange={props.onAccentColorChange}
            onInstanceDeleted={props.onInstanceDeleted}
            onAuthSettingsChange={props.onAuthSettingsChange}
            pageMode={true}
            isGameRunning={props.isGameRunning}
            onMovingDataChange={props.onMovingDataChange}
          />
        </Suspense>
      </div>
    </motion.div>
  );
};
