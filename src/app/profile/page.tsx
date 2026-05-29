'use client'

import { useEffect, useState } from 'react'
import { motion } from 'framer-motion'
import { useSearchParams } from 'next/navigation'
import { Card } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'
import { useAuthStore } from '@/stores/authStore'
import { api } from '@/lib/api'

export default function ProfilePage() {
  const searchParams = useSearchParams()
  const userId = searchParams.get('id')
  const { user, token } = useAuthStore()
  const [profile, setProfile] = useState<any>(null)
  const [matches, setMatches] = useState<any[]>([])

  useEffect(() => {
    async function load() {
      try {
        const uid = userId ? Number(userId) : user?.id
        if (uid && token) {
          const p = await api.getUser(uid)
          setProfile(p)
          const s = await api.getUserStats(uid)
          if (s.recentMatches) setMatches(s.recentMatches.slice(0, 5))
        }
      } catch {}
      if (!profile) {
        setProfile({ nickname: user?.nickname || '玩家', mmr: user?.mmr || 1500, winCount: user?.winCount || 0, loseCount: user?.loseCount || 0 })
      }
    }
    load()
  }, [userId, user, token])

  if (!profile) return null

  return (
    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }}>
      <div className="flex items-center gap-6 mb-6">
        <div className="w-20 h-20 rounded-full bg-primary/20 flex items-center justify-center text-3xl font-bold text-primary">{profile.nickname?.[0] || '?'}</div>
        <div>
          <h2 className="text-2xl font-bold text-white">{profile.nickname || '玩家'}</h2>
          <p className="text-sm text-primary font-bold mt-1">{profile.mmr || 1500} MMR</p>
          <p className="text-xs text-surface-400">胜: {profile.winCount || 0} 负: {profile.loseCount || 0}</p>
        </div>
        <Button variant="secondary">添加好友</Button>
      </div>

      <div className="grid grid-cols-4 gap-4 mb-6">
        {[
          { label: '胜场', value: profile.winCount || 0 },
          { label: '负场', value: profile.loseCount || 0 },
          { label: '总场次', value: (profile.winCount || 0) + (profile.loseCount || 0) },
          { label: 'MMR', value: profile.mmr || 1500 },
        ].map((s, i) => (
          <Card key={i} variant="default" className="text-center">
            <p className="text-2xl font-bold text-primary">{s.value}</p>
            <p className="text-xs text-surface-400 mt-1">{s.label}</p>
          </Card>
        ))}
      </div>

      <Card variant="default">
        <h3 className="text-base font-bold text-white mb-4">最近比赛</h3>
        {matches.length === 0 ? (
          <p className="text-sm text-surface-400">暂无比赛记录</p>
        ) : matches.map((m, i) => (
          <div key={i} className="flex items-center gap-3 px-3 py-2.5 rounded-lg hover:bg-hover transition-colors">
            <span className={'w-2 h-2 rounded-full ' + (m.isWinner ? 'bg-primary' : 'bg-red-500')} />
            <span className="flex-1 text-sm text-white">{m.mapName || 'de_dust2'}</span>
            <span className={'text-sm font-bold ' + (m.isWinner ? 'text-primary' : 'text-red-400')}>{m.score || '--'}</span>
          </div>
        ))}
      </Card>
    </motion.div>
  )
}
