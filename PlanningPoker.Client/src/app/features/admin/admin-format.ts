/** "3 d 4 h", "2 h 5 m", "7 m", "42 s": the two largest units that are not zero. */
export function formatUptime(totalSeconds: number): string {
  const seconds = Math.max(0, Math.floor(totalSeconds));
  const days = Math.floor(seconds / 86400);
  const hours = Math.floor((seconds % 86400) / 3600);
  const minutes = Math.floor((seconds % 3600) / 60);
  if (days > 0) {
    return `${days} d ${hours} h`;
  }
  if (hours > 0) {
    return `${hours} h ${minutes} m`;
  }
  if (minutes > 0) {
    return `${minutes} m`;
  }
  return `${seconds} s`;
}
