import { clsx, type ClassValue } from 'clsx'
import { twMerge } from 'tailwind-merge'

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

export function formatDate(d: Date | string): string {
  const date = typeof d === 'string' ? new Date(d) : d
  return date.toLocaleDateString('zh-CN', { month: '2-digit', day: '2-digit' })
}

export function formatPlayTime(seconds: number): string {
  if (seconds < 60) return `${seconds}秒`
  if (seconds < 3600) return `${Math.floor(seconds / 60)}分${seconds % 60}秒`
  return `${Math.floor(seconds / 3600)}时${Math.floor((seconds % 3600) / 60)}分`
}

export function getRankByMMR(mmr: number): { name: string; color: string; tier: number } {
  if (mmr >= 2000) return { name: 'Grandmaster', color: '#A855F7', tier: 7 }
  if (mmr >= 1700) return { name: 'Master', color: '#22D97E', tier: 6 }
  if (mmr >= 1400) return { name: 'Diamond', color: '#06D8A0', tier: 5 }
  if (mmr >= 1100) return { name: 'Platinum', color: '#3B82F6', tier: 4 }
  if (mmr >= 800) return { name: 'Gold', color: '#F59E0B', tier: 3 }
  if (mmr >= 500) return { name: 'Silver', color: '#9EAE9F', tier: 2 }
  return { name: 'Bronze', color: '#CD7F32', tier: 1 }
}
