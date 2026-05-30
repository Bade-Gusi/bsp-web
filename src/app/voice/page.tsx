'use client'

import { useState, useEffect } from 'react'
import { motion } from 'framer-motion'
import { Card } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'
import { Modal } from '@/components/ui/Modal'
import { Input } from '@/components/ui/Input'
import { toast } from '@/components/ui/Toast'
import { useAuthStore } from '@/stores/authStore'

export default function VoicePage() {
  const [rooms, setRooms] = useState<any[]>([])
  const [myRooms, setMyRooms] = useState<any[]>([])
  const [showCreate, setShowCreate] = useState(false)
  const [roomName, setRoomName] = useState('')
  const [roomPw, setRoomPw] = useState('')
  const [joinCode, setJoinCode] = useState('')
  const [loading, setLoading] = useState(true)
  const { user } = useAuthStore()

  const load = async () => {
    setLoading(true)
    try {
      const res = await fetch('/api/voice/rooms')
      if (res.ok) { const data = await res.json(); setRooms(Array.isArray(data) ? data : []) }
    } catch {}
    setLoading(false)
  }

  useEffect(() => { load() }, [])

  const token = useAuthStore(s => s.token)
  const headers = { 'Content-Type': 'application/json', ...(token ? { 'Authorization': `Bearer ${token}` } : {}) }

  const handleCreate = async () => {
    if (!roomName.trim()) { toast('请输入房间名称', 'error'); return }
    try {
      const res = await fetch('/api/voice/rooms', { method: 'POST', headers, body: JSON.stringify({ name: roomName.trim(), password: roomPw || undefined }) })
      if (!res.ok) throw new Error()
      setShowCreate(false); setRoomName(''); setRoomPw('')
      toast('创建成功', 'success'); load()
    } catch { toast('创建失败', 'error') }
  }

  const handleJoin = async () => {
    if (!joinCode.trim()) { toast('请输入房间码', 'error'); return }
    try {
      const res = await fetch(`/api/voice/rooms/${joinCode.trim()}/join`, { method: 'POST', headers, body: '{}' })
      if (!res.ok) throw new Error()
      toast('已加入', 'success'); setJoinCode(''); load()
    } catch { toast('加入失败', 'error') }
  }

  const handleLeave = async (code: string) => {
    try {
      const res = await fetch(`/api/voice/rooms/${code}/leave`, { method: 'POST', headers })
      if (!res.ok) throw new Error()
      toast('已离开', 'info'); load()
    } catch { toast('操作失败', 'error') }
  }

  return (
    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }}>
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-xl font-bold text-white">语音房间</h2>
        <div className="flex gap-2">
          <Button size="sm" onClick={() => setShowCreate(true)}>创建房间</Button>
        </div>
      </div>

      <div className="grid grid-cols-[2fr_1fr] gap-6">
        <div className="space-y-3">
          <h3 className="text-sm font-semibold text-surface-300">所有房间</h3>
          {loading ? <div className="flex justify-center py-8"><div className="w-5 h-5 border-2 border-primary border-t-transparent rounded-full animate-spin" /></div>
            : rooms.length === 0 ? <p className="text-surface-400 text-sm">暂无语音房间</p>
            : rooms.map((r, i) => (
              <Card key={i} variant="default" className="flex items-center justify-between">
                <div>
                  <p className="text-sm text-white font-semibold">{r.name || r.roomCode}</p>
                  <p className="text-xs text-surface-400">{r.currentUsers || 0}/{r.maxUsers || 10} 人 · {r.roomCode}</p>
                </div>
                <div className="flex gap-2">
                  <Button size="sm" onClick={() => window.open(`/voice/${r.roomCode}`, '_blank')}>加入语音</Button>
                  <Button variant="ghost" size="sm" onClick={() => handleLeave(r.roomCode)}>离开</Button>
                </div>
              </Card>
            ))}
        </div>

        <div className="space-y-4">
          <Card variant="default">
            <h3 className="text-sm font-bold text-white mb-3">加入房间</h3>
            <div className="flex gap-2">
              <input value={joinCode} onChange={e => setJoinCode(e.target.value)} placeholder="输入房间码"
                className="flex-1 bg-elevated border border-border rounded-md px-3 py-2 text-sm text-white placeholder:text-surface-500 outline-none focus:border-primary" />
              <Button size="sm" onClick={handleJoin}>加入</Button>
            </div>
          </Card>
          <Card variant="default">
            <p className="text-xs text-surface-400">我的房间</p>
            <p className="text-sm text-surface-300 mt-2">{myRooms.length === 0 ? '未加入任何房间' : myRooms.map(r => r.roomCode).join(', ')}</p>
          </Card>
        </div>
      </div>

      <Modal isOpen={showCreate} onClose={() => setShowCreate(false)} title="创建语音房间" size="sm">
        <div className="space-y-4">
          <Input label="房间名称" placeholder="输入房间名" value={roomName} onChange={e => setRoomName(e.target.value)} />
          <Input label="密码（可选）" type="password" placeholder="留空则公开" value={roomPw} onChange={e => setRoomPw(e.target.value)} />
          <Button className="w-full" onClick={handleCreate}>创建</Button>
        </div>
      </Modal>
    </motion.div>
  )
}
