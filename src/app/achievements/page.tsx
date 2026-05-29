'use client'

import { motion } from 'framer-motion'
import { useState, useEffect } from 'react'
import { useAuthStore } from '@/stores/authStore'
import { api } from '@/lib/api'

interface Achievement {
  id: number
  name: string
  description: string
  icon: string
  unlocked: boolean
  unlockedDate: string
}

const ACHIEVEMENT_ICONS: Record<string, string> = {
  '初入背水': '🏆', '连胜专家': '🔥', '头部猎手': '🎯', '防御大师': '🛡️',
  '影响力': '⭐', '背水老将': '🎖️', '社交达人': '🤝', '战赜宝库': '🏅',
}

export default function AchievementsPage() {
  const [achievements, setAchievements] = useState<Achievement[]>([])
  const [loading, setLoading] = useState(true)
  const { user } = useAuthStore()

  useEffect(() => {
    async function load() {
      if (!user?.id) { setLoading(false); return }
      try {
        const data = await api.getAchievements(user.id)
        if (Array.isArray(data) && data.length > 0) {
          setAchievements(data.map((a: any) => ({
            id: a.achievementId || a.id,
            name: a.name || '',
            description: a.description || '',
            icon: ACHIEVEMENT_ICONS[a.name] || '🏅',
            unlocked: a.unlocked || false,
            unlockedDate: a.unlockedDate || '',
          })))
        }
      } catch {}
      setLoading(false)
    }
    load()
  }, [user?.id])

  const unlocked = achievements.filter(a => a.unlocked).length
  const total = achievements.length
  const pct = total > 0 ? Math.round(unlocked / total * 100) : 0

  return (
    <motion.div className="min-h-screen bg-surface p-6"
      initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }}>
      <div className="mx-auto max-w-5xl">
        <motion.h1 className="mb-6 text-3xl font-bold text-white"
          initial={{ opacity: 0, x: -20 }} animate={{ opacity: 1, x: 0 }}>成就</motion.h1>

        <div className="card-hover rounded-xl bg-card p-6 mb-6">
          <div className="flex items-center justify-between mb-2">
            <span className="text-sm text-surface-400">总进度</span>
            <span className="text-sm text-white font-medium">
              {loading ? '加载中...' : `${unlocked}/${total} (${pct}%)`}
            </span>
          </div>
          <div className="h-2 bg-surface-700 rounded-full overflow-hidden">
            <motion.div className="h-full bg-gradient-to-r from-primary to-accent rounded-full"
              initial={{ width: 0 }} animate={{ width: loading ? 0 : `${pct}%` }}
              transition={{ duration: 0.8, ease: 'easeOut' }} />
          </div>
        </div>

        {loading ? (
          <div className="flex items-center justify-center py-20">
            <div className="w-8 h-8 rounded-full border-[3px] border-primary border-t-transparent animate-spin" />
          </div>
        ) : achievements.length === 0 ? (
          <div className="text-center py-20 text-surface-400">
            <p className="text-4xl mb-4">🏅</p>
            <p>暂无成就数据</p>
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
            {achievements.map(ach => (
              <div key={ach.id}
                className={'card-hover rounded-xl p-5 transition-all duration-300 ' +
                  (ach.unlocked ? 'bg-card' : 'bg-surface-700/30 opacity-50')}>
                <div className="text-3xl mb-3">{ach.icon}</div>
                <h3 className="text-sm font-semibold text-white mb-1">{ach.name}</h3>
                <p className="text-xs text-surface-400 mb-2">{ach.description}</p>
                {ach.unlocked ? (
                  <span className="text-xs text-primary">已解锁 {ach.unlockedDate}</span>
                ) : (
                  <span className="text-xs text-surface-500">未解锁</span>
                )}
              </div>
            ))}
          </div>
        )}
      </div>
    </motion.div>
  )
}
