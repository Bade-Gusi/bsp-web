'use client'

import { motion } from 'framer-motion'
import { useState, useEffect } from 'react'
import { useAuthStore } from '@/stores/authStore'
import { api } from '@/lib/api'

interface Entry {
  userId: number
  rank: number
  nickname: string
  mmr: number
  winCount: number
  loseCount: number
  totalKills: number
  totalDeaths: number
  totalGames: number
  headshotCount: number
}

const SORT_TABS = ['Rating', '胜率', '击杀', '场次']

export default function LeaderboardPage() {
  const [activeTab, setActiveTab] = useState('Rating')
  const [entries, setEntries] = useState<Entry[]>([])
  const [loading, setLoading] = useState(true)
  const [page, setPage] = useState(1)
  const { user } = useAuthStore()

  useEffect(() => {
    async function load() {
      setLoading(true)
      try {
        const data = await api.getLeaderboard(page)
        if (Array.isArray(data)) {
          setEntries(data.map((e: any, i: number) => ({
            userId: e.userId || e.id || 0,
            rank: e.rank || (page - 1) * 50 + i + 1,
            nickname: e.nickname || '玩家',
            mmr: e.mmr || 0,
            winCount: e.winCount || 0,
            loseCount: e.loseCount || 0,
            totalKills: e.totalKills || e.kills || 0,
            totalDeaths: e.totalDeaths || e.deaths || 0,
            totalGames: e.totalGames || e.matches || 0,
            headshotCount: e.headshotCount || 0,
          })))
        }
      } catch {}
      setLoading(false)
    }
    load()
  }, [page])

  const sorted = [...entries].sort((a, b) => {
    if (activeTab === '胜率') {
      const ar = a.winCount + a.loseCount > 0 ? a.winCount / (a.winCount + a.loseCount) : 0
      const br = b.winCount + b.loseCount > 0 ? b.winCount / (b.winCount + b.loseCount) : 0
      return br - ar
    }
    if (activeTab === '击杀') return b.totalKills - a.totalKills
    if (activeTab === '场次') return b.totalGames - a.totalGames
    return b.mmr - a.mmr
  }).map((e, i) => ({ ...e, rank: i + 1 }))

  const getRankStyle = (rank: number) => {
    if (rank === 1) return 'text-yellow-400'
    if (rank === 2) return 'text-gray-300'
    if (rank === 3) return 'text-amber-600'
    return 'text-surface-400'
  }

  return (
    <motion.div className="min-h-screen bg-surface p-6"
      initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }}>
      <div className="mx-auto max-w-4xl">
        <motion.h1 className="mb-6 text-3xl font-bold text-white"
          initial={{ opacity: 0, x: -20 }} animate={{ opacity: 1, x: 0 }}>排行榜</motion.h1>

        <div className="flex gap-2 rounded-xl bg-card p-1.5 mb-6">
          {SORT_TABS.map(tab => (
            <button key={tab} onClick={() => setActiveTab(tab)}
              className={'flex-1 rounded-lg px-6 py-2.5 text-sm font-medium transition-all ' +
                (activeTab === tab ? 'bg-primary text-black' : 'text-surface-400 hover:text-white hover:bg-surface-600')}>
              {tab}
            </button>
          ))}
        </div>

        {loading ? (
          <div className="flex items-center justify-center py-20">
            <div className="w-8 h-8 rounded-full border-[3px] border-primary border-t-transparent animate-spin" />
          </div>
        ) : sorted.length === 0 ? (
          <div className="text-center py-20 text-surface-400">
            <p className="text-4xl mb-4">📊</p>
            <p>暂无排行数据</p>
          </div>
        ) : (
          <div className="space-y-2">
            {sorted.map((entry, idx) => {
              const medals = ['🥇', '🥈', '🥉']
              const wr = entry.winCount + entry.loseCount > 0
                ? Math.round(entry.winCount / (entry.winCount + entry.loseCount) * 100) : 0
              return (
                <div key={entry.userId}
                  className={'flex items-center gap-4 rounded-xl px-5 py-3 transition-all ' +
                    (idx < 3 ? 'bg-card ring-1 ring-primary/20' : 'bg-card hover:bg-surface-600')}>
                  <span className={'w-8 text-center text-lg font-bold ' + getRankStyle(entry.rank)}>
                    {idx < 3 ? medals[idx] : `#${entry.rank}`}
                  </span>
                  <div className="flex h-10 w-10 items-center justify-center rounded-full bg-primary/20 text-sm text-primary font-semibold">
                    {entry.nickname[0]}
                  </div>
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium text-white truncate">
                      {entry.nickname}
                      {entry.userId === user?.id && <span className="text-xs text-primary ml-2">(你)</span>}
                    </p>
                  </div>
                  <div className="flex items-center gap-6 text-sm shrink-0">
                    <span className="text-primary font-bold w-14 text-right">{entry.mmr}</span>
                    <span className="text-surface-400 text-xs w-10 text-right">{wr}%</span>
                    <span className="text-surface-400 text-xs w-14 text-right">{entry.totalKills}</span>
                    <span className="text-surface-400 text-xs w-12 text-right">{entry.totalGames}</span>
                  </div>
                </div>
              )
            })}
          </div>
        )}

        {entries.length > 0 && (
          <div className="flex items-center justify-center gap-4 mt-6">
            <button onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page <= 1}
              className="px-4 py-2 rounded-lg text-sm bg-surface-800 text-surface-300 disabled:opacity-40 hover:bg-surface-700">
              上一页
            </button>
            <span className="text-sm text-surface-400">第 {page} 页</span>
            <button onClick={() => setPage(p => p + 1)}
              className="px-4 py-2 rounded-lg text-sm bg-surface-800 text-surface-300 hover:bg-surface-700">
              下一页
            </button>
          </div>
        )}
      </div>
    </motion.div>
  )
}
