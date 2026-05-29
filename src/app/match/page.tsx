'use client'

import { useState } from 'react'
import { motion } from 'framer-motion'
import { Card } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'
import { api } from '@/lib/api';
import { useAuthStore } from '@/stores/authStore';

export default function MatchPage() {
  const [mode, setMode] = useState('competitive')
  const [queuing, setQueuing] = useState(false)

  const modes = [
    { id: 'competitive', label: '竞技' },
    { id: 'casual', label: '休闲' },
    { id: 'deathmatch', label: '死斗' },
  ]

  const regions = ['中国', 'HK', 'JP']

  return (
    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }}>
      <h2 className="text-xl font-bold text-white mb-6">快速匹配</h2>
      <div className="grid grid-cols-[2fr_1fr] gap-6">
        <div className="space-y-4">
          <Card variant="default">
            <p className="text-sm text-surface-300 mb-3">游戏模式</p>
            <div className="flex gap-2">
              {modes.map((m) => (
                <button key={m.id} onClick={() => setMode(m.id)}
                  className={'px-5 py-2 rounded-md text-sm font-semibold transition-all ' + (mode === m.id ? 'bg-primary text-surface' : 'bg-elevated text-surface-300 border border-border')}>
                  {m.label}
                </button>
              ))}
            </div>

            <p className="text-sm text-surface-300 mt-5 mb-3">区域</p>
            <div className="flex gap-2">
              {regions.map((r) => (
                <button key={r}
                  className="px-5 py-2 rounded-md text-sm font-semibold bg-elevated text-surface-300 border border-border">
                  {r}
                </button>
              ))}
            </div>

            <Button size="lg" className="w-full mt-6 h-14 text-lg" onClick={() => setQueuing(!queuing)}>
              {queuing ? '取消匹配' : '开始匹配'}
            </Button>
          </Card>
        </div>

        <Card variant="default">
          <h3 className="text-base font-bold text-white mb-4">匹配队列</h3>
          <p className="text-sm text-surface-400">当前等待: 12 人</p>
          <p className="text-sm text-surface-400 mt-1">平均 MMR: 1,200</p>
        </Card>
      </div>
    </motion.div>
  )
}
