/**
 * Shared formatting utilities
 */

/**
 * Format bytes to human-readable size string
 * @param bytes - Number of bytes
 * @returns Formatted string like "256 MB" or "1.5 GB"
 */
export const formatBytes = (bytes: number): string => {
  if (bytes === 0) return '0 B';
  const k = 1024;
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return `${(bytes / Math.pow(k, i)).toFixed(i > 1 ? 1 : 0)} ${sizes[i]}`;
};

/**
 * Format a number with commas for thousands separator
 * @param num - Number to format
 * @returns Formatted string like "1,234,567"
 */
export const formatNumber = (num: number): string => {
  return num.toLocaleString();
};

/**
 * @param seconds - Duration in seconds
 * @returns Formatted string like "2h 30m" or "45s"
 */
export const formatDuration = (seconds: number): string => {
  if (seconds < 60) return `${Math.round(seconds)}s`;
  if (seconds < 3600) {
    const mins = Math.floor(seconds / 60);
    const secs = Math.round(seconds % 60);
    return secs > 0 ? `${mins}m ${secs}s` : `${mins}m`;
  }
  const hours = Math.floor(seconds / 3600);
  const mins = Math.floor((seconds % 3600) / 60);
  return mins > 0 ? `${hours}h ${mins}m` : `${hours}h`;
};
