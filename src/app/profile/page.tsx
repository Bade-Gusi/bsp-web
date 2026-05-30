'use client'

import { useEffect, useState } from 'react'
import { motion } from 'framer-motion'
import { Card } from '@/components/ui/Card'
import { Badge } from '@/components/ui/Badge'
import { useAuthStore } from '@/stores/authStore'
import { api } from '@/lib/api'

export default function ProfilePage() {
  const { user, token } = useAuthStore()
  const [stats, setStats] = useState<any>(null)
  const [matches, setMatches] = useState<any[]>([])

  useEffect(() => {
    if (!user || !token) return
    api.getUserStats(user.id).then(s => setStats(s)).catch(() => {})
    api.getMatches(1).then(m => { if (Array.isArray(m)) setMatches(m.slice(0, 5)) }).catch(() => {})
  }, [user, token])

  if (!user) return null

  const winRate = stats ? Math.round((stats.winCount / (stats.winCount + stats.loseCount || 1)) * 100) : '--'

  return (
    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }}>
      <div className="flex items-center gap-6 mb-8">
        <div className="w-20 h-20 rounded-full bg-primary/20 flex items-center justify-center text-3xl font-bold text-primary">{user.nickname?.[0] || user.username[0]}</div>
        <div>
          <h2 className="text-2xl font-bold text-white">{user.nickname || user.username}</h2>
          <div className="flex items-center gap-2 mt-1">
            <Badge variant="primary">{stats?.mmr || user.mmr || 1000} MMR</Badge>
            <span className="text-xs text-surface-400">UID: {user.id}</span>
          </div>
          {stats && <p className="text-xs text-surface-400 mt-1">胜 {stats.winCount} 负 {stats.loseCount} · 胜率 {winRate}%</p>}
        </div>
      </div>

      <div className="grid grid-cols-4 gap-4 mb-6">
        {[
          { label: '胜场', value: stats?.winCount ?? '--' },
          { label: '负场', value: stats?.loseCount ?? '--' },
          { label: 'Rating', value: stats?.mmr ?? user.mmr ?? '--' },
          { label: '爆头', value: stats?.headshotCount ?? '--' },
        ].map((s, i) => (
          <Card key={i} variant="default" className="text-center py-4">
            <p className="text-2xl font-bold text-primary">{s.value}</p>
            <p className="text-xs text-surface-400 mt-1">{s.label}</p>
          </Card>
        ))}
      </div>

      <Card variant="default">
        <h3 className="text-base font-bold text-white mb-4">最近比赛</h3>
        {matches.length === 0 ? <p className="text-sm text-surface-400">暂无比赛记录</p>
          : matches.map((m, i) => (
            <div key={i} className="flex items-center gap-3 px-3 py-2.5 rounded-lg hover:bg-hover transition-colors">
              <span className={'w-2 h-2 rounded-full ' + (m.isWin ? 'bg-primary' : 'bg-red-500')} />
              <span className="flex-1 text-sm text-white">{m.mapName || m.map || 'de_dust2'}</span>
              <span className={'text-sm font-bold ' + (m.isWin ? 'text-primary' : 'text-red-400')}>{m.isWin ? '胜利' : '失败'}</span>
            </div>
          ))}
      </Card>

      <div className="mt-6 p-4 bg-card rounded-md border border-border">
        <p className="text-xs text-surface-400">背水对战平台 v2.0.0 Aurora</p>
        <p className="text-xs text-surface-500 mt-1">Build {new Date().toISOString().slice(0, 10)}</p>
      </div>
    </motion.div>
  )
}
