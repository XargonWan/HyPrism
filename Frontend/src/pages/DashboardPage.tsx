import React, { useState, useRef, useEffect, useCallback, useMemo, memo } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { useTranslation } from 'react-i18next';
import { Play, Download, Loader2, X, RefreshCw, Copy, Edit3, User, ExternalLink, Calendar, Github, Newspaper } from 'lucide-react';
import { useAccentColor } from '../contexts/AccentColorContext';
import { useAnimatedGlass } from '../contexts/AnimatedGlassContext';
import { ipc } from '@/lib/ipc';
import { DiscordIcon } from '../components/icons/DiscordIcon';
import { formatBytes } from '../utils/format';

export type VersionStatus = {
  status: 'installed' | 'update_available' | 'not_installed' | 'unknown';
  installedVersion?: number;
  latestVersion?: number;
};

type NewsFilter = 'all' | 'hytale' | 'hyprism';

interface EnrichedNewsItem {
  title: string;
  excerpt?: string;
  url?: string;
  date?: string;
  author?: string;
  imageUrl?: string;
  source?: 'hytale' | 'hyprism';
}

interface DashboardPageProps {
  // Profile
  username: string;
  uuid: string;
  isEditing: boolean;
  launcherVersion: string;
  updateAvailable: boolean;
  avatarRefreshTrigger: number;
  onEditToggle: (editing: boolean) => void;
  onUserChange: (name: string) => void;
  onOpenProfileEditor: () => void;
  onLauncherUpdate: () => void;
  // Game state
  isDownloading: boolean;
  downloadState: 'downloading' | 'extracting' | 'launching';
  canCancel: boolean;
  isGameRunning: boolean;
  isVersionInstalled: boolean;
  isCheckingInstalled: boolean;
  versionStatus: VersionStatus | null;
  progress: number;
  downloaded: number;
  total: number;
  launchState: string;
  launchDetail: string;
  // Version (kept for action-button logic)
  currentBranch: string;
  currentVersion: number;
  availableVersions: number[];
  installedVersions: { version: number; isValid: boolean }[];
  isLoadingVersions: boolean;
  onBranchChange: (branch: string) => void;
  onVersionChange: (version: number) => void;
  // Actions
  onPlay: () => void;
  onDownload: () => void;
  onUpdate: () => void;
  onDuplicate: () => void;
  onExit: () => void;
  onCancelDownload: () => void;
  // News
  getNews?: (count: number) => Promise<EnrichedNewsItem[]>;
  newsDisabled?: boolean;
}

const pageVariants = {
  initial: { opacity: 0, y: 12 },
  animate: { opacity: 1, y: 0 },
  exit: { opacity: 0, y: -12 },
};

const cardVariants = {
  hidden: { opacity: 0, y: 16 },
  visible: (i: number) => ({
    opacity: 1,
    y: 0,
    transition: { delay: i * 0.04, duration: 0.3, ease: 'easeOut' },
  }),
};

export const DashboardPage: React.FC<DashboardPageProps> = memo((props) => {
  const { t } = useTranslation();
  const { accentColor, accentTextColor } = useAccentColor();
  const { animatedGlass } = useAnimatedGlass();
  const [editValue, setEditValue] = useState(props.username);
  const [localAvatar, setLocalAvatar] = useState<string | null>(null);
  const [showCancelButton, setShowCancelButton] = useState(false);

  // News state
  const [news, setNews] = useState<EnrichedNewsItem[]>([]);
  const [newsLoading, setNewsLoading] = useState(true);
  const [newsRefreshing, setNewsRefreshing] = useState(false);
  const [newsError, setNewsError] = useState<string | null>(null);
  const [newsFilter, setNewsFilter] = useState<NewsFilter>('all');
  const [newsLimit, setNewsLimit] = useState(12);
  const newsScrollRef = useRef<HTMLDivElement>(null);

  useEffect(() => { setEditValue(props.username); }, [props.username]);

  useEffect(() => {
    ipc.profile.get().then(p => { if (p.avatarPath) setLocalAvatar(p.avatarPath); }).catch(() => {});
  }, [props.uuid, props.avatarRefreshTrigger]);

  useEffect(() => {
    if (!props.isDownloading) setShowCancelButton(false);
  }, [props.isDownloading]);

  // News fetching
  const fetchNews = useCallback(async (count: number, reset = false) => {
    if (!props.getNews) return;
    if (news.length === 0) setNewsLoading(true);
    else setNewsRefreshing(true);
    setNewsError(null);
    try {
      const items = await props.getNews(count);
      setNews((prev) => {
        if (reset) return items;
        const seen = new Map<string, EnrichedNewsItem>();
        prev.forEach((item) => seen.set(item.url || item.title, item));
        (items || []).forEach((item) => seen.set(item.url || item.title, item));
        return Array.from(seen.values());
      });
    } catch (err) {
      if (news.length === 0) setNewsError(err instanceof Error ? err.message : 'Failed to fetch news');
    } finally {
      setNewsLoading(false);
      setNewsRefreshing(false);
    }
  }, [props.getNews, news.length]);

  useEffect(() => {
    if (props.getNews && !props.newsDisabled) {
      fetchNews(newsLimit, newsLimit === 12 && news.length === 0);
    }
  }, [newsLimit, props.getNews, props.newsDisabled]);

  const filteredNews = useMemo(
    () => (newsFilter === 'all' ? news : news.filter(item => item.source === newsFilter)),
    [newsFilter, news]
  );

  const handleNewsScroll = useCallback(() => {
    if (!newsScrollRef.current || newsLoading || newsRefreshing) return;
    const { scrollTop, scrollHeight, clientHeight } = newsScrollRef.current;
    if (scrollHeight - scrollTop - clientHeight < 200) {
      setNewsLimit((prev) => prev + 6);
    }
  }, [newsLoading, newsRefreshing]);

  const openLink = useCallback((url: string) => { ipc.browser.open(url); }, []);

  const handleSave = () => {
    if (editValue.trim() && editValue.length <= 16) {
      props.onUserChange(editValue.trim());
      props.onEditToggle(false);
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') handleSave();
    else if (e.key === 'Escape') { setEditValue(props.username); props.onEditToggle(false); }
  };

  const getLaunchStateLabel = () => {
    const stateKey = `launch.state.${props.launchState}`;
    const translated = t(stateKey);
    return translated !== stateKey ? translated : (props.launchState || t('launch.state.preparing'));
  };

  // ── Action Button ─────────────────────────────────────────────────
  const renderActionButton = () => {
    if (props.isGameRunning) {
      return (
        <button
          disabled
          className="w-full h-14 flex items-center justify-center gap-3 font-black text-lg tracking-tight bg-gradient-to-r from-red-600 to-red-500 text-white rounded-2xl cursor-not-allowed opacity-90"
        >
          <Loader2 size={18} className="animate-spin" />
          <span>{t('main.running')}</span>
        </button>
      );
    }

    if (props.isDownloading) {
      return (
        <div
          className={`w-full h-14 flex items-center justify-center relative overflow-hidden rounded-2xl ${props.canCancel ? 'cursor-pointer' : 'cursor-default'}`}
          style={{ background: 'rgba(255,255,255,0.08)' }}
          onMouseEnter={() => props.canCancel && setShowCancelButton(true)}
          onMouseLeave={() => setShowCancelButton(false)}
          onClick={() => showCancelButton && props.canCancel && props.onCancelDownload()}
        >
          <div
            className="absolute inset-0 transition-all duration-300"
            style={{ width: `${Math.min(props.progress, 100)}%`, backgroundColor: `${accentColor}40` }}
          />
          {showCancelButton && props.canCancel ? (
            <div className="relative z-10 flex items-center gap-2 text-red-500 hover:text-red-400 transition-colors">
              <X size={18} />
              <span className="text-sm font-bold uppercase">{t('main.cancel')}</span>
            </div>
          ) : (
            <div className="relative z-10 flex items-center gap-2">
              <Loader2 size={16} className="animate-spin text-white" />
              <span className="text-sm font-bold text-white">{getLaunchStateLabel()}</span>
            </div>
          )}
        </div>
      );
    }

    if (props.isCheckingInstalled) {
      return (
        <button disabled className="w-full h-14 flex items-center justify-center gap-3 font-black text-lg bg-white/10 text-white/50 cursor-not-allowed rounded-2xl">
          <Loader2 size={18} className="animate-spin" />
          <span>{t('main.checking')}</span>
        </button>
      );
    }

    if (props.isVersionInstalled && props.versionStatus?.status === 'update_available' && props.currentVersion === 0) {
      return (
        <div className="flex items-center gap-2 w-full">
          <button
            onClick={props.onUpdate}
            className="flex-1 h-14 flex items-center justify-center gap-2 font-black text-sm bg-gradient-to-r from-blue-500 to-cyan-500 text-white rounded-2xl hover:brightness-110 active:scale-[0.98] transition-all"
          >
            <RefreshCw size={16} />
            <span>{t('main.update')}</span>
          </button>
          <button
            onClick={props.onPlay}
            className="flex-1 h-14 flex items-center justify-center gap-2 font-black text-lg rounded-2xl hover:brightness-110 active:scale-[0.98] transition-all"
            style={{ background: `linear-gradient(135deg, ${accentColor}, ${accentColor}cc)`, color: accentTextColor }}
          >
            <Play size={18} fill="currentColor" />
            <span>{t('main.play')}</span>
          </button>
        </div>
      );
    }

    if (props.isVersionInstalled) {
      return (
        <button
          onClick={props.onPlay}
          className="w-full h-14 flex items-center justify-center gap-3 font-black text-lg rounded-2xl hover:brightness-110 active:scale-[0.98] transition-all"
          style={{ background: `linear-gradient(135deg, ${accentColor}, ${accentColor}cc)`, color: accentTextColor }}
        >
          <Play size={18} fill="currentColor" />
          <span>{t('main.play')}</span>
        </button>
      );
    }

    if (props.currentVersion > 0 && props.versionStatus?.installedVersion === props.currentVersion) {
      return (
        <button
          onClick={props.onDuplicate}
          className="w-full h-14 flex items-center justify-center gap-3 font-black text-lg rounded-2xl bg-gradient-to-r from-purple-500 to-violet-600 text-white hover:brightness-110 active:scale-[0.98] transition-all"
        >
          <Copy size={18} />
          <span>{t('main.duplicate')}</span>
        </button>
      );
    }

    return (
      <button
        onClick={props.onDownload}
        className="w-full h-14 flex items-center justify-center gap-3 font-black text-lg rounded-2xl bg-gradient-to-r from-green-500 to-emerald-600 text-white hover:brightness-110 active:scale-[0.98] transition-all"
      >
        <Download size={18} />
        <span>{t('main.download')}</span>
      </button>
    );
  };

  // ── Render ─────────────────────────────────────────────────────────
  return (
    <motion.div
      variants={pageVariants}
      initial="initial"
      animate="animate"
      exit="exit"
      transition={{ duration: 0.3, ease: 'easeOut' }}
      className="h-full flex flex-col px-4 pt-14 pb-28"
    >
      {/* Top Row: Profile left · Social right */}
      <div className="w-full flex justify-between items-start mb-4 flex-shrink-0">
        {/* Profile */}
        <motion.div
          initial={{ opacity: 0, x: -20 }}
          animate={{ opacity: 1, x: 0 }}
          transition={{ delay: 0.1 }}
          className="flex items-center gap-3"
        >
          <motion.button
            whileHover={{ scale: 1.05 }}
            whileTap={{ scale: 0.95 }}
            onClick={props.onOpenProfileEditor}
            className="w-12 h-12 rounded-full overflow-hidden border-2 flex items-center justify-center flex-shrink-0"
            style={{ borderColor: accentColor, backgroundColor: localAvatar ? 'transparent' : `${accentColor}20` }}
            title={t('main.editProfile')}
          >
            {localAvatar ? (
              <img src={localAvatar} className="w-full h-full object-cover object-[center_20%]" alt="Avatar" />
            ) : (
              <User size={20} style={{ color: accentColor }} />
            )}
          </motion.button>
          <div className="flex flex-col">
            <div className="flex items-center gap-2">
              {props.isEditing ? (
                <div className="flex items-center gap-2">
                  <input
                    type="text"
                    value={editValue}
                    onChange={(e) => setEditValue(e.target.value)}
                    onKeyDown={handleKeyDown}
                    maxLength={16}
                    autoFocus
                    className="bg-[#151515] text-white text-lg font-bold px-3 py-1 rounded-lg border outline-none w-36"
                    style={{ borderColor: `${accentColor}4d` }}
                    onFocus={(e) => { e.currentTarget.style.borderColor = accentColor; }}
                    onBlur={(e) => { e.currentTarget.style.borderColor = `${accentColor}4d`; }}
                  />
                  <motion.button whileTap={{ scale: 0.9 }} onClick={handleSave}
                    className="p-1.5 rounded-lg" style={{ backgroundColor: `${accentColor}33`, color: accentColor }}>
                    <Edit3 size={14} />
                  </motion.button>
                </div>
              ) : (
                <>
                  <span className="text-lg font-bold text-white">{props.username}</span>
                  <motion.button whileTap={{ scale: 0.9 }} onClick={() => props.onEditToggle(true)}
                    className="p-1 rounded text-white/30 hover:text-white/60 transition-colors">
                    <Edit3 size={12} />
                  </motion.button>
                </>
              )}
            </div>
            <div className="flex items-center gap-2">
              <span className="text-xs text-white/30">HyPrism {props.launcherVersion}</span>
              {props.updateAvailable && (
                <motion.button whileTap={{ scale: 0.95 }} onClick={props.onLauncherUpdate}
                  className="text-[10px] font-medium transition-colors hover:opacity-80" style={{ color: accentColor }}>
                  <Download size={10} className="inline mr-1" />{t('main.updateAvailable')}
                </motion.button>
              )}
            </div>
          </div>
        </motion.div>

        {/* Social Links */}
        <motion.div
          initial={{ opacity: 0, x: 20 }}
          animate={{ opacity: 1, x: 0 }}
          transition={{ delay: 0.1 }}
          className="flex items-center gap-2"
        >
          <button
            onClick={() => ipc.browser.open('https://discord.gg/hyprism')}
            className="p-2 rounded-xl hover:bg-[#5865F2]/20 transition-all active:scale-95"
            title={t('main.joinDiscord')}
          >
            <DiscordIcon size={22} className="drop-shadow-lg" />
          </button>
          <button
            onClick={() => ipc.browser.open('https://github.com/yyyumeniku/HyPrism')}
            className="w-9 h-9 rounded-xl flex items-center justify-center text-white/50 hover:text-white hover:bg-white/10 active:scale-95 transition-all"
            title={t('main.gitHubRepository')}
          >
            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="currentColor"><path d="M12 0c-6.626 0-12 5.373-12 12 0 5.302 3.438 9.8 8.207 11.387.599.111.793-.261.793-.577v-2.234c-3.338.726-4.033-1.416-4.033-1.416-.546-1.387-1.333-1.756-1.333-1.756-1.089-.745.083-.729.083-.729 1.205.084 1.839 1.237 1.839 1.237 1.07 1.834 2.807 1.304 3.492.997.107-.775.418-1.305.762-1.604-2.665-.305-5.467-1.334-5.467-5.931 0-1.311.469-2.381 1.236-3.221-.124-.303-.535-1.524.117-3.176 0 0 1.008-.322 3.301 1.23.957-.266 1.983-.399 3.003-.404 1.02.005 2.047.138 3.006.404 2.291-1.552 3.297-1.23 3.297-1.23.653 1.653.242 2.874.118 3.176.77.84 1.235 1.911 1.235 3.221 0 4.609-2.807 5.624-5.479 5.921.43.372.823 1.102.823 2.222v3.293c0 .319.192.694.801.576 4.765-1.589 8.199-6.086 8.199-11.386 0-6.627-5.373-12-12-12z"/></svg>
          </button>
        </motion.div>
      </div>

      {/* ── Main content: Left (logo + button) | Right (news) ─────── */}
      <div className="flex-1 flex gap-4 min-h-0">
        {/* Left Side — Logo + Action */}
        <div className="flex flex-col items-center justify-center w-[380px] flex-shrink-0">
          <motion.div
            initial={{ opacity: 0, scale: 0.9 }}
            animate={{ opacity: 1, scale: 1 }}
            transition={{ delay: 0.2, type: 'spring', stiffness: 200 }}
            className="flex flex-col items-center"
          >
            {/* Logo */}
            <div className="select-none mb-2">
              <h1 className="text-7xl tracking-tighter leading-tight font-black drop-shadow-xl" style={{ fontFamily: 'system-ui, -apple-system, sans-serif' }}>
                <span className="text-white">Hy</span>
                <motion.span
                  className="bg-clip-text text-transparent bg-[length:200%_auto]"
                  style={{
                    backgroundImage: `linear-gradient(90deg, ${accentColor}, #22d3ee, #e879f9, ${accentColor})`,
                    filter: `drop-shadow(0 0 30px ${accentColor}66)`,
                  }}
                  animate={{ backgroundPosition: ['0%', '200%'] }}
                  transition={{ duration: 8, repeat: Infinity, ease: 'linear' }}
                >Prism</motion.span>
              </h1>
            </div>

            {/* Educational label */}
            <AnimatePresence>
              {!props.isDownloading && !props.isGameRunning && (
                <motion.p
                  initial={{ opacity: 0 }}
                  animate={{ opacity: 1 }}
                  exit={{ opacity: 0 }}
                  className="text-white/40 text-xs mb-5 text-center"
                >
                  {t('main.educational')}{' '}
                  <button
                    onClick={() => ipc.browser.open('https://hytale.com')}
                    className="font-semibold hover:underline cursor-pointer"
                    style={{ color: accentColor }}
                  >
                    {t('main.buyIt')}
                  </button>
                </motion.p>
              )}
            </AnimatePresence>

            {/* Action Button */}
            <div className="w-full max-w-[280px]">
              {renderActionButton()}

              {/* Progress details below button */}
              <AnimatePresence>
                {props.isDownloading && props.launchState !== 'complete' && (
                  <motion.div
                    initial={{ opacity: 0, y: 8 }}
                    animate={{ opacity: 1, y: 0 }}
                    exit={{ opacity: 0, y: 8 }}
                    transition={{ duration: 0.2 }}
                    className="mt-3 w-full"
                  >
                    <div className={`${animatedGlass ? 'bg-black/60 backdrop-blur-md' : 'bg-[#1a1a1a]/95'} rounded-xl px-3 py-2 border border-white/5`}>
                      <div className="h-1.5 w-full bg-white/10 rounded-full overflow-hidden">
                        <div
                          className="h-full rounded-full transition-all duration-300"
                          style={{ width: `${Math.min(props.progress, 100)}%`, backgroundColor: accentColor }}
                        />
                      </div>
                      <div className="flex justify-between items-center mt-1.5 text-[10px]">
                        <span className="text-white/60 truncate max-w-[180px]">
                          {props.launchDetail
                            ? (t(props.launchDetail) !== props.launchDetail
                              ? t(props.launchDetail).replace('{0}', `${Math.min(Math.round(props.progress), 100)}`)
                              : props.launchDetail)
                            : getLaunchStateLabel()}
                        </span>
                        <span className="text-white/50 font-mono">
                          {props.total > 0
                            ? `${formatBytes(props.downloaded)} / ${formatBytes(props.total)}`
                            : `${Math.min(Math.round(props.progress), 100)}%`}
                        </span>
                      </div>
                    </div>
                  </motion.div>
                )}
              </AnimatePresence>
            </div>
          </motion.div>
        </div>

        {/* ── Right Side — News Feed ──────────────────────────────── */}
        <motion.div
          initial={{ opacity: 0, x: 20 }}
          animate={{ opacity: 1, x: 0 }}
          transition={{ delay: 0.25, duration: 0.4 }}
          className={`flex-1 flex flex-col min-w-0 overflow-hidden rounded-2xl ${animatedGlass ? 'glass-panel' : 'glass-panel-static-solid'}`}
        >
          {/* Header + Filters */}
          <div className="flex items-center justify-between px-4 py-3 border-b border-white/[0.06] flex-shrink-0">
            <div className="flex items-center gap-2">
              <Newspaper size={16} className="text-white/60" />
              <h3 className="text-white font-medium text-sm">{t('news.title')}</h3>
              {newsRefreshing && <RefreshCw size={12} className="animate-spin text-white/40" />}
            </div>
            <div className="flex gap-1.5">
              {(['all', 'hytale', 'hyprism'] as NewsFilter[]).map((f) => (
                <button
                  key={f}
                  onClick={() => setNewsFilter(f)}
                  className={`px-3 py-1 text-[10px] rounded-lg font-medium transition-all ${
                    newsFilter === f ? '' : 'text-white/40 hover:text-white/60 hover:bg-white/5'
                  }`}
                  style={newsFilter === f ? { backgroundColor: `${accentColor}20`, color: accentColor } : undefined}
                >
                  {f === 'all' ? t('news.all') : f === 'hytale' ? t('news.hytale') : t('news.hyprism')}
                </button>
              ))}
            </div>
          </div>

          {/* News body */}
          {props.newsDisabled ? (
            <div className="flex-1 flex items-center justify-center">
              <div className="text-center">
                <Newspaper size={36} className="text-white/15 mx-auto mb-3" />
                <p className="text-white/30 text-sm">{t('news.disabled')}</p>
              </div>
            </div>
          ) : newsLoading ? (
            <div className="flex-1 flex items-center justify-center">
              <RefreshCw size={24} className="animate-spin" style={{ color: accentColor }} />
            </div>
          ) : newsError ? (
            <div className="flex-1 flex items-center justify-center">
              <div className="text-center">
                <p className="text-red-400 text-sm mb-3">{newsError}</p>
                <button onClick={() => fetchNews(newsLimit, true)}
                  className="px-4 py-1.5 bg-white/10 hover:bg-white/15 rounded-lg transition-colors text-sm">
                  {t('news.tryAgain')}
                </button>
              </div>
            </div>
          ) : filteredNews.length === 0 ? (
            <div className="flex-1 flex items-center justify-center">
              <p className="text-white/30 text-sm">{t('news.noNewsFound')}</p>
            </div>
          ) : (
            <div ref={newsScrollRef} onScroll={handleNewsScroll} className="flex-1 overflow-y-auto p-3">
              <div className="grid grid-cols-2 gap-3">
                <AnimatePresence>
                  {filteredNews.map((item, index) => (
                    <motion.button
                      key={item.url || item.title}
                      custom={index}
                      variants={cardVariants}
                      initial="hidden"
                      animate="visible"
                      whileTap={{ scale: 0.98 }}
                      onClick={() => item.url && openLink(item.url)}
                      className="relative group rounded-xl overflow-hidden text-left cursor-pointer"
                      style={{
                        aspectRatio: '16/10',
                        background: 'rgba(255,255,255,0.03)',
                        border: '1px solid rgba(255,255,255,0.06)',
                      }}
                    >
                      {/* Background */}
                      {item.source === 'hyprism' ? (
                        <div className="absolute inset-0 bg-gradient-to-br from-[#2c2c2e] to-[#1c1c1e] flex items-center justify-center">
                          <Github size={48} className="text-white/10" />
                        </div>
                      ) : item.imageUrl ? (
                        <img src={item.imageUrl} alt={item.title}
                          className="absolute inset-0 w-full h-full object-cover transition-transform duration-500 group-hover:scale-105" />
                      ) : null}

                      {/* Gradient */}
                      <div className="absolute inset-0 bg-gradient-to-t from-black/90 via-black/40 to-transparent" />

                      {/* Source Badge */}
                      {item.source && (
                        <div className="absolute top-2 left-2 z-10">
                          <span
                            className="px-1.5 py-0.5 text-[9px] font-bold uppercase rounded-md"
                            style={{
                              backgroundColor: item.source === 'hytale' ? 'rgba(255,168,69,0.9)' : `${accentColor}dd`,
                              color: item.source === 'hytale' ? '#000' : accentTextColor,
                            }}
                          >
                            {item.source === 'hytale' ? 'Hytale' : 'HyPrism'}
                          </span>
                        </div>
                      )}

                      {/* External link icon on hover */}
                      <div className="absolute top-2 right-2 z-10 opacity-0 group-hover:opacity-100 translate-x-1 group-hover:translate-x-0 transition-all duration-300">
                        <span
                          className="flex items-center justify-center w-6 h-6 rounded-lg"
                          style={{
                            background: 'linear-gradient(135deg, rgba(255,255,255,0.15) 0%, rgba(255,255,255,0.06) 100%)',
                            backdropFilter: 'blur(16px)',
                            border: '1px solid rgba(255,255,255,0.15)',
                          }}
                        >
                          <ExternalLink size={11} className="text-white" />
                        </span>
                      </div>

                      {/* Card Content */}
                      <div className="absolute bottom-0 left-0 right-0 p-3 z-10">
                        <h3 className="text-white font-bold text-xs line-clamp-2 mb-0.5 drop-shadow-lg">{item.title}</h3>
                        <p className="text-white/50 text-[10px] line-clamp-1 mb-1 drop-shadow">{item.excerpt}</p>
                        <div className="flex items-center gap-2 text-white/35 text-[9px]">
                          {item.author && <span className="flex items-center gap-0.5"><User size={8} />{item.author}</span>}
                          {item.date && <span className="flex items-center gap-0.5"><Calendar size={8} />{item.date}</span>}
                        </div>
                      </div>
                    </motion.button>
                  ))}
                </AnimatePresence>
              </div>

              <div className="text-center py-3 mt-1">
                <button
                  onClick={() => openLink('https://hytale.com/news')}
                  className="font-semibold hover:underline cursor-pointer text-[10px]"
                  style={{ color: accentColor }}
                >
                  {t('news.readMore')} →
                </button>
              </div>
            </div>
          )}
        </motion.div>
      </div>
    </motion.div>
  );
});

DashboardPage.displayName = 'DashboardPage';
