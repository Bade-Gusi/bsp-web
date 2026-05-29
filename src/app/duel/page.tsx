'use client'

import { useState, useEffect } from 'react'
import { motion } from 'framer-motion'
import { Card } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'
import { Badge } from '@/components/ui/Badge'
import { api } from '@/lib/api'
import { useAuthStore } from '@/stores/authStore'

interface DuelInvite {
  id: number
  fromUserId: number
  fromNickname: string
  status: string
  createdAt: string
}

export default function DuelPage() {
  const [tab, setTab] = useState('queue')
  const [inQueue, setInQueue] = useState(false)
  const [queueLoading, setQueueLoading] = useState(false)
  const [invites, setInvites] = useState<DuelInvite[]>([])
  const [history, setHistory] = useState<any[]>([])
  const [opponentName, setOpponentName] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const { user } = useAuthStore()

  const fetchInvites = async () => {
    try {
      const data = await api.getDuelInvites()
      if (Array.isArray(data)) {
        setInvites(data.map((i: any) => ({
          id: i.inviteId || i.id,
          fromUserId: i.fromUserId || i.senderId,
          fromNickname: i.fromNickname || i.senderName || '未知',
          status: i.status || 'pending',
          createdAt: i.createdAt || '',
        })))
      }
    } catch {}
  }

  useEffect(() => {
    if (tab === 'invite') fetchInvites()
  }, [tab])

  const handleQueueJoin = async () => {
    setQueueLoading(true)
    try {
      if (inQueue) {
        await api.leaveDuelQueue()
        setInQueue(false)
      } else {
        await api.joinDuelQueue()
        setInQueue(true)
      }
    } catch {}
    setQueueLoading(false)
  }

  const handleSendInvite = async () => {
    setError('')
    if (!opponentName.trim()) { setError('请输入对手用户名'); return }
    setLoading(true)
    try {
      const results = await api.searchUsers(opponentName.trim())
      if (Array.isArray(results) && results.length > 0) {
        await api.sendDuelInvite(results[0].id || results[0].userId)
        setError('邀约已发送')
        setOpponentName('')
      } else {
        setError('未找到该用户')
      }
    } catch (err: any) { setError(err.message || '发送失败') }
    finally { setLoading(false) }
  }

  const handleAccept = async (id: number) => {
    try { await api.acceptDuel(id); fetchInvites() } catch {}
  }

  const handleReject = async (id: number) => {
    try { await api.rejectDuel(id); fetchInvites() } catch {}
  }

  const tabs = [
    { id: 'queue', label: '匹配队列' },
    { id: 'invite', label: '发起邀约' },
    { id: 'history', label: '对战记录' },
  ]

  return (
    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }}>
      <h2 className="text-xl font-bold text-white mb-6">1v1 对战</h2>

      <div className="flex gap-2 mb-6">
        {tabs.map(t => (
          <button key={t.id} onClick={() => setTab(t.id)}
            className={'px-5 py-2 rounded-md text-sm font-semibold transition-all ' +
              (tab === t.id ? 'bg-primary text-surface' : 'bg-elevated border border-border text-surface-300')}>
            {t.label}
          </button>
        ))}
      </div>

      {tab === 'queue' && (
        <Card variant="default" className="text-center py-12">
          <p className="text-4xl mb-4">⚔️</p>
          <h3 className="text-lg font-bold text-white mb-2">1v1 匹配</h3>
          <p className="text-sm text-surface-300 mb-6">系统将根据 MMR 匹配实力相当的对手</p>
          <Button size="lg" onClick={handleQueueJoin} loading={queueLoading}>
            {inQueue ? '离开队列' : '加入队列'}
          </Button>
          {inQueue && (
            <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} className="mt-4">
              <div className="flex items-center justify-center gap-2 text-sm text-primary">
                <div className="w-2 h-2 rounded-full bg-primary animate-pulse" />
                搜索中...
              </div>
            </motion.div>
          )}
        </Card>
      )}

      {tab === 'invite' && (
        <div className="space-y-4">
          <Card variant="default">
            <h3 className="text-base font-bold text-white mb-4">发起邀约</h3>
            <div className="flex gap-2">
              <input placeholder="输入对手用户名" value={opponentName} onChange={e => setOpponentName(e.target.value)}
                onKeyDown={e => e.key === 'Enter' && handleSendInvite()}
                className="flex-1 bg-elevated border border-border rounded-md px-3 py-2.5 text-sm text-white placeholder:text-surface-500 outline-none focus:border-primary" />
              <Button onClick={handleSendInvite} loading={loading}>邀约</Button>
            </div>
            {error && <p className={'text-sm mt-3 ' + (error.includes('已发送') ? 'text-primary' : 'text-red-400')}>{error}</p>}
          </Card>

          <Card variant="default">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-base font-bold text-white">收到的邀约</h3>
              <Badge variant="primary">{invites.filter(i => i.status === 'pending').length}</Badge>
            </div>
            {invites.length === 0 ? (
              <p className="text-sm text-surface-400">暂无邀约</p>
            ) : (
              invites.filter(i => i.status === 'pending').map(inv => (
                <div key={inv.id} className="flex items-center justify-between px-3 py-2.5 rounded-lg hover:bg-hover">
                  <span className="text-sm text-white">{inv.fromNickname}</span>
                  <div className="flex gap-2">
                    <Button size="sm" onClick={() => handleAccept(inv.id)}>接受</Button>
                    <Button size="sm" variant="ghost" onClick={() => handleReject(inv.id)}>拒绝</Button>
                  </div>
                </div>
              ))
            )}
          </Card>
        </div>
      )}

      {tab === 'history' && (
        <Card variant="default">
          <h3 className="text-base font-bold text-white mb-4">对战记录</h3>
          {history.length === 0 ? (
            <p className="text-sm text-surface-400 py-8 text-center">暂无对战记录</p>
          ) : (
            history.map((d, i) => (
              <div key={i} className="flex items-center gap-3 px-3 py-2.5 rounded-lg hover:bg-hover">
                <span className={'w-2 h-2 rounded-full ' + (d.result === '胜' ? 'bg-primary' : 'bg-red-500')} />
                <span className="flex-1 text-sm text-white">{d.opponent}</span>
                <span className="text-xs text-surface-400">{d.date}</span>
                <span className={'text-sm font-bold ' + (d.result === '胜' ? 'text-primary' : 'text-red-400')}>{d.score}</span>
              </div>
            ))
          )}
        </Card>
      )}
    </motion.div>
  )
}
