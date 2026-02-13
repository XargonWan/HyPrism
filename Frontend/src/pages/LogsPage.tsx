import React, { useState, useEffect, useCallback, useRef } from 'react';
import { motion } from 'framer-motion';
import { useTranslation } from 'react-i18next';
import { FileText, RefreshCw, Copy, Check, Download, Search } from 'lucide-react';
import { useAccentColor } from '../contexts/AccentColorContext';
import { invoke } from '@/lib/ipc';

type LogLevel = 'all' | 'INF' | 'SUC' | 'WRN' | 'ERR' | 'DBG';

interface LogEntry {
  timestamp: string;
  level: string;
  category: string;
  message: string;
  raw: string;
}

const pageVariants = {
  initial: { opacity: 0, y: 12 },
  animate: { opacity: 1, y: 0 },
  exit: { opacity: 0, y: -12 },
};

const parseLogEntry = (line: string): LogEntry => {
  // Format: "HH:mm:ss | LVL | Category | Message"
  const parts = line.split(' | ');
  if (parts.length >= 4) {
    return {
      timestamp: parts[0],
      level: parts[1],
      category: parts[2],
      message: parts.slice(3).join(' | '),
      raw: line,
    };
  }
  return {
    timestamp: '',
    level: 'INF',
    category: 'Unknown',
    message: line,
    raw: line,
  };
};

const getLevelColor = (level: string): string => {
  switch (level) {
    case 'ERR': return 'text-red-400';
    case 'WRN': return 'text-yellow-400';
    case 'SUC': return 'text-green-400';
    case 'DBG': return 'text-gray-500';
    case 'INF':
    default: return 'text-gray-300';
  }
};

// No longer using getLevelBgColor - logs have transparent background

export const LogsPage: React.FC = () => {
  const { t } = useTranslation();
  const { accentColor } = useAccentColor();
  const [logs, setLogs] = useState<LogEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [filter, setFilter] = useState<LogLevel>('all');
  const [searchQuery, setSearchQuery] = useState('');
  const [copied, setCopied] = useState(false);
  const [autoRefresh, setAutoRefresh] = useState(true);
  const [selectedIndices, setSelectedIndices] = useState<Set<number>>(new Set());
  const [isDragging, setIsDragging] = useState(false);
  const scrollRef = useRef<HTMLDivElement>(null);
  const autoScrollRef = useRef(true);

  const fetchLogs = useCallback(async () => {
    if (logs.length === 0) setLoading(true);
    else setIsRefreshing(true);
    try {
      const rawLogs = await invoke<string[]>('hyprism:logs:get', { count: 100 });
      const parsed = (rawLogs || []).map(parseLogEntry);
      setLogs(parsed);
    } catch (err) {
      console.error('Failed to fetch logs:', err);
    } finally {
      setLoading(false);
      setIsRefreshing(false);
    }
  }, [logs.length]);

  // Initial fetch
  useEffect(() => {
    fetchLogs();
  }, []);

  // Auto-refresh every 2 seconds
  useEffect(() => {
    if (!autoRefresh) return;
    const interval = setInterval(fetchLogs, 2000);
    return () => clearInterval(interval);
  }, [autoRefresh, fetchLogs]);

  // Auto-scroll to bottom when new logs arrive
  useEffect(() => {
    if (autoScrollRef.current && scrollRef.current) {
      scrollRef.current.scrollTop = scrollRef.current.scrollHeight;
    }
  }, [logs]);

  // Detect manual scroll to disable auto-scroll
  const handleScroll = useCallback(() => {
    if (!scrollRef.current) return;
    const { scrollTop, scrollHeight, clientHeight } = scrollRef.current;
    autoScrollRef.current = scrollHeight - scrollTop - clientHeight < 50;
  }, []);

  const filteredLogs = logs.filter(log => {
    const matchesLevel = filter === 'all' || log.level === filter;
    const matchesSearch = searchQuery === '' || 
      log.message.toLowerCase().includes(searchQuery.toLowerCase()) ||
      log.category.toLowerCase().includes(searchQuery.toLowerCase());
    return matchesLevel && matchesSearch;
  });

  // Handle mouse down on log entry - start selection
  const handleLogMouseDown = useCallback((index: number, event: React.MouseEvent) => {
    event.preventDefault(); // Prevent text selection
    
    if (event.shiftKey && selectedIndices.size > 0) {
      // Shift-click: select range
      const lastSelected = Math.max(...selectedIndices);
      const start = Math.min(lastSelected, index);
      const end = Math.max(lastSelected, index);
      const newSelection = new Set(selectedIndices);
      for (let i = start; i <= end; i++) {
        newSelection.add(i);
      }
      setSelectedIndices(newSelection);
    } else if (event.ctrlKey || event.metaKey) {
      // Ctrl/Cmd-click: toggle individual selection
      const newSelection = new Set(selectedIndices);
      if (newSelection.has(index)) {
        newSelection.delete(index);
      } else {
        newSelection.add(index);
      }
      setSelectedIndices(newSelection);
    } else {
      // Regular click: toggle if same single selection, otherwise select this one
      if (selectedIndices.size === 1 && selectedIndices.has(index)) {
        setSelectedIndices(new Set());
      } else {
        setSelectedIndices(new Set([index]));
        setIsDragging(true);
      }
    }
  }, [selectedIndices]);

  // Handle mouse enter during drag
  const handleLogMouseEnter = useCallback((index: number) => {
    if (isDragging) {
      setSelectedIndices(prev => new Set([...prev, index]));
    }
  }, [isDragging]);

  // Handle mouse up - stop dragging
  useEffect(() => {
    const handleMouseUp = () => setIsDragging(false);
    window.addEventListener('mouseup', handleMouseUp);
    return () => window.removeEventListener('mouseup', handleMouseUp);
  }, []);

  const handleCopy = useCallback(async () => {
    let text: string;
    if (selectedIndices.size > 0) {
      // Copy only selected logs
      const sortedIndices = Array.from(selectedIndices).sort((a, b) => a - b);
      text = sortedIndices.map(i => filteredLogs[i]?.raw).filter(Boolean).join('\n');
      setSelectedIndices(new Set()); // Clear selection after copy
    } else {
      // Copy all filtered logs
      text = filteredLogs.map(l => l.raw).join('\n');
    }
    try {
      await navigator.clipboard.writeText(text);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch (err) {
      console.error('Failed to copy logs:', err);
    }
  }, [filteredLogs, selectedIndices]);

  const handleExport = useCallback(() => {
    const text = filteredLogs.map(l => l.raw).join('\n');
    const blob = new Blob([text], { type: 'text/plain' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `hyprism-logs-${new Date().toISOString().slice(0, 10)}.txt`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  }, [filteredLogs]);

  const levelFilters: { value: LogLevel; label: string; color: string }[] = [
    { value: 'all', label: t('logs.filter.all'), color: 'text-white/70' },
    { value: 'INF', label: t('logs.filter.info'), color: 'text-gray-300' },
    { value: 'SUC', label: t('logs.filter.success'), color: 'text-green-400' },
    { value: 'WRN', label: t('logs.filter.warning'), color: 'text-yellow-400' },
    { value: 'ERR', label: t('logs.filter.error'), color: 'text-red-400' },
    { value: 'DBG', label: t('logs.filter.debug'), color: 'text-gray-500' },
  ];

  // Background style matching navigation menu
  const panelStyle: React.CSSProperties = {
    background: 'rgba(28, 28, 30, 0.98)',
    border: '1px solid rgba(255,255,255,0.08)',
  };

  return (
    <motion.div
      variants={pageVariants}
      initial="initial"
      animate="animate"
      exit="exit"
      transition={{ duration: 0.3, ease: 'easeOut' }}
      className="h-full flex flex-col px-8 pt-6 pb-28"
    >
      {/* Header */}
      <div className="flex items-center justify-between mb-4 flex-shrink-0">
        <div className="flex items-center gap-3">
          <FileText size={22} className="text-white/80" />
          <h1 className="text-xl font-semibold text-white/90">
            {t('logs.title')}
          </h1>
        </div>

        <div className="flex items-center gap-2">
          {/* Auto-refresh toggle */}
          <button
            onClick={() => setAutoRefresh(!autoRefresh)}
            className={`px-3 py-2 rounded-lg text-xs font-medium transition-all ${
              autoRefresh 
                ? 'bg-green-500/20 text-green-400 border border-green-500/30' 
                : 'text-white/40 hover:bg-white/10'
            }`}
            style={!autoRefresh ? panelStyle : undefined}
          >
            {t('logs.auto')}
          </button>

          {/* Manual refresh */}
          <button
            onClick={fetchLogs}
            disabled={isRefreshing}
            className="p-2 rounded-lg text-white/60 hover:text-white/80 transition-all disabled:opacity-50"
            style={panelStyle}
            title={t('logs.refresh')}
          >
            <RefreshCw size={16} className={isRefreshing ? 'animate-spin' : ''} />
          </button>

          {/* Copy (selected or all) */}
          <button
            onClick={handleCopy}
            className="p-2 rounded-lg text-white/60 hover:text-white/80 transition-all"
            style={panelStyle}
            title={selectedIndices.size > 0 ? t('logs.copySelected') : t('logs.copy')}
          >
            {copied ? <Check size={16} className="text-green-400" /> : <Copy size={16} />}
          </button>

          {/* Export */}
          <button
            onClick={handleExport}
            className="p-2 rounded-lg text-white/60 hover:text-white/80 transition-all"
            style={panelStyle}
            title={t('logs.export')}
          >
            <Download size={16} />
          </button>
        </div>
      </div>

      {/* Filters and Search */}
      <div className="flex items-center gap-4 mb-4 flex-shrink-0">
        {/* Search */}
        <div className="relative flex-1 max-w-sm">
          <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-white/40" />
          <input
            type="text"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            placeholder={t('logs.searchPlaceholder')}
            className="w-full h-10 pl-9 pr-4 rounded-lg text-white/80 placeholder-white/30 text-sm focus:outline-none"
            style={{ ...panelStyle, border: '1px solid rgba(255,255,255,0.1)' }}
          />
        </div>

        {/* Level filters - same height as search */}
        <div className="flex items-center gap-1 h-10 rounded-lg px-1" style={panelStyle}>
          {levelFilters.map(({ value, label, color }) => (
            <button
              key={value}
              onClick={() => setFilter(value)}
              className={`px-3 h-8 rounded-md text-xs font-medium transition-all ${
                filter === value
                  ? 'bg-white/15'
                  : 'hover:bg-white/10'
              } ${color}`}
            >
              {label}
            </button>
          ))}
        </div>
      </div>

      {/* Log count and selection info */}
      <div className="text-xs text-white/40 mb-2 flex items-center gap-2">
        <span>
          {t('logs.showing', {
            count: filteredLogs.length,
            total: logs.length
          })}
        </span>
        {selectedIndices.size > 0 && (
          <span className="text-white/60">
            • {t('logs.selected', { count: selectedIndices.size })}
          </span>
        )}
      </div>

      {/* Logs container */}
      <div
        ref={scrollRef}
        onScroll={handleScroll}
        className="flex-1 overflow-y-auto rounded-xl font-mono text-xs"
        style={{ ...panelStyle, scrollBehavior: 'smooth' }}
      >
        {loading && logs.length === 0 ? (
          <div className="flex items-center justify-center h-full text-white/40">
            <RefreshCw size={20} className="animate-spin mr-2" />
            {t('logs.loading')}
          </div>
        ) : filteredLogs.length === 0 ? (
          <div className="flex items-center justify-center h-full text-white/40">
            {searchQuery || filter !== 'all' 
              ? t('logs.noResults')
              : t('logs.empty')}
          </div>
        ) : (
          <div className="p-2 space-y-0.5 select-none">
            {filteredLogs.map((log, i) => (
              <div
                key={i}
                onMouseDown={(e) => handleLogMouseDown(i, e)}
                onMouseEnter={() => handleLogMouseEnter(i)}
                className={`flex items-start gap-2 px-2 py-1 rounded border cursor-pointer transition-colors ${
                  selectedIndices.has(i)
                    ? 'bg-white/15 border-white/20'
                    : 'border-transparent hover:bg-white/5'
                }`}
              >
                <span className="text-white/30 shrink-0 w-16">{log.timestamp}</span>
                <span className={`shrink-0 w-8 font-semibold ${getLevelColor(log.level)}`}>
                  {log.level}
                </span>
                <span 
                  className="shrink-0 w-24 truncate"
                  style={{ color: accentColor }}
                >
                  {log.category}
                </span>
                <span className="text-white/80 break-all flex-1">{log.message}</span>
              </div>
            ))}
          </div>
        )}
      </div>
    </motion.div>
  );
};
