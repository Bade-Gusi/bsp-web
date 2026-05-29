'use client'

import { useState } from 'react'
import { motion } from 'framer-motion'
import { Card } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'
import { api } from '@/lib/api';
import { useAuthStore } from '@/stores/authStore';

export default function DuelPage() {
  const [tab, setTab] = useState('invite')
  const [inQueue, setInQueue] = useState(false)

  const history = [
    { opponent: 'PlayerOne', result: '胜', score: '16:12', date: '今天' },
    { opponent: 'ProAimer', result: '负', score: '8:16', date: '昨天' },
  ]

  return (
    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }}>
      <h2 className="text-xl font-bold text-white mb-6">1v1 对战</h2>

      <div className="flex gap-2 mb-6">
        {[{ id: 'invite', label: '发起邀约' }, { id: 'queue', label: '匹配队列' }, { id: 'history', label: '对战记录' }].map((t) => (
          <button key={t.id} onClick={() => setTab(t.id)}
            className={'px-5 py-2 rounded-md text-sm font-semibold transition-all ' + (tab === t.id ? 'bg-primary text-surface' : 'bg-elevated border border-border text-surface-300')}>
            {t.label}
          </button>
        ))}
      </div>

      {tab === 'queue' && (
        <Card variant="default" className="text-center py-12">
          <p className="text-4xl mb-4">⚔️</p>
          <h3 className="text-lg font-bold text-white mb-2">1v1 匹配</h3>
          <p className="text-sm text-surface-300 mb-6">系统将根据 MMR 匹配对手</p>
          <Button size="lg" onClick={() => setInQueue(!inQueue)}>
            {inQueue ? '离开队列' : '加入队列'}
          </Button>
        </Card>
      )}

      {tab === 'invite' && (
        <Card variant="default">
          <h3 className="text-base font-bold text-white mb-4">发起邀约</h3>
          <p className="text-sm text-surface-400">搜索玩家并发起1v1邀约</p>
        </Card>
      )}

      {tab === 'history' && (
        <Card variant="default">
          <h3 className="text-base font-bold text-white mb-4">对战记录</h3>
          {history.map((d, i) => (
            <div key={i} className="flex items-center gap-3 px-3 py-2.5 rounded-lg hover:bg-hover">
              <span className={'w-2 h-2 rounded-full ' + (d.result === '胜' ? 'bg-primary' : 'bg-red-500')} />
              <span className="flex-1 text-sm text-white">{d.opponent}</span>
              <span className="text-xs text-surface-400">{d.date}</span>
              <span className={'text-sm font-bold ' + (d.result === '胜' ? 'text-primary' : 'text-red-400')}>{d.score}</span>
            </div>
          ))}
        </Card>
      )}
    </motion.div>
  )
}
