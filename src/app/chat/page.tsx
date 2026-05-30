'use client'

import { useState, useRef, useEffect } from 'react'
import { motion } from 'framer-motion'
import { Card } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'
import { toast } from '@/components/ui/Toast'
import { api } from '@/lib/api'
import { useAuthStore } from '@/stores/authStore'

export default function ChatPage() {
  const [messages, setMessages] = useState<any[]>([])
  const [msg, setMsg] = useState('')
  const [userList, setUserList] = useState<any[]>([])
  const [loading, setLoading] = useState(true)
  const [sending, setSending] = useState(false)
  const bottomRef = useRef<HTMLDivElement>(null)
  const { user } = useAuthStore()

  useEffect(() => {
    // 加载最近消息
    setMessages([
      { id: 1, from: '系统', text: '欢迎来到背水平台聊天室', time: new Date().toISOString() },
      { id: 2, from: 'ProPlayer', text: '有人来打竞技吗？', time: new Date(Date.now() - 60000).toISOString() },
    ])
    setUserList([
      { id: 1, name: 'ProPlayer', online: true },
      { id: 2, name: 'CS2Master', online: true },
    ])
    setLoading(false)
  }, [])

  useEffect(() => { bottomRef.current?.scrollIntoView({ behavior: 'smooth' }) }, [messages])

  const sendMessage = async () => {
    const text = msg.trim()
    if (!text || sending) return
    setSending(true)
    try {
      // 1.5秒冷却防止刷屏
      setMessages(prev => [...prev, { id: Date.now(), from: user?.nickname || '我', text, time: new Date().toISOString(), isMine: true }])
      setMsg('')
      await new Promise(r => setTimeout(r, 1500))
    } catch { toast('发送失败', 'error') }
    setSending(false)
  }

  const formatTime = (t: string) => {
    const d = new Date(t)
    return d.toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit' })
  }

  return (
    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} className="flex gap-6 h-[calc(100vh-8rem)]">
      {/* 聊天区域 */}
      <Card variant="default" className="flex-1 flex flex-col">
        <div className="flex items-center justify-between pb-3 border-b border-border mb-3">
          <h3 className="text-base font-bold text-white">大厅聊天</h3>
          <Button variant="ghost" size="sm">刷新</Button>
        </div>

        <div className="flex-1 overflow-y-auto space-y-2 mb-3 px-1">
          {loading ? <div className="flex justify-center py-8"><div className="w-5 h-5 border-2 border-primary border-t-transparent rounded-full animate-spin" /></div>
            : messages.length === 0 ? <p className="text-surface-400 text-sm text-center py-8">暂无消息</p>
            : messages.map(m => (
              <div key={m.id} className={'flex ' + (m.isMine ? 'justify-end' : 'justify-start')}>
                <div className={'max-w-[70%] px-3 py-2 rounded-lg text-sm ' + (m.isMine ? 'bg-primary text-surface' : 'bg-elevated text-white')}>
                  {!m.isMine && <p className="text-xs text-primary font-semibold mb-0.5">{m.from}</p>}
                  <p>{m.text}</p>
                  <p className={'text-[10px] mt-0.5 ' + (m.isMine ? 'text-surface/60' : 'text-surface-400')}>{formatTime(m.time)}</p>
                </div>
              </div>
            ))}
          <div ref={bottomRef} />
        </div>

        <div className="flex gap-2 pt-3 border-t border-border">
          <input value={msg} onChange={e => setMsg(e.target.value)} placeholder={sending ? '请勿频繁发送...' : '输入消息...'}
            className="flex-1 bg-elevated border border-border rounded-md px-3 py-2.5 text-sm text-white placeholder:text-surface-500 outline-none focus:border-primary"
            onKeyDown={e => e.key === 'Enter' && sendMessage()} disabled={sending} />
          <Button onClick={sendMessage} disabled={sending || !msg.trim()}>{sending ? '冷却中...' : '发送'}</Button>
        </div>
      </Card>

      {/* 在线列表 */}
      <Card variant="default" className="w-56 shrink-0">
        <h3 className="text-sm font-bold text-white mb-3">在线 ({userList.length})</h3>
        {userList.map(u => (
          <div key={u.id} className="flex items-center gap-2 px-2 py-1.5 rounded-lg hover:bg-hover transition-colors mb-0.5">
            <span className={'w-1.5 h-1.5 rounded-full ' + (u.online ? 'bg-primary' : 'bg-surface-500')} />
            <span className="text-sm text-white truncate">{u.name}</span>
          </div>
        ))}
      </Card>
    </motion.div>
  )
}
