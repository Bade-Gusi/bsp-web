'use client'

import { useState, useRef, useEffect, useCallback } from 'react'
import { motion } from 'framer-motion'
import { Card } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'
import { toast } from '@/components/ui/Toast'
import { useAuthStore } from '@/stores/authStore'
import * as signalR from '@microsoft/signalr'

const API = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001'

interface ChatMsg { id: number; from: string; text: string; time: string; isMine?: boolean }

export default function ChatPage() {
  const [messages, setMessages] = useState<ChatMsg[]>([])
  const [users, setUsers] = useState<{ id: number; name: string; online: boolean }[]>([])
  const [msg, setMsg] = useState('')
  const [sending, setSending] = useState(false)
  const [connected, setConnected] = useState(false)
  const bottomRef = useRef<HTMLDivElement>(null)
  const connRef = useRef<signalR.HubConnection | null>(null)
  const { user, token } = useAuthStore()

  // 连接 SignalR ChatHub
  useEffect(() => {
    if (!token) return
    const conn = new signalR.HubConnectionBuilder()
      .withUrl(`${API}/hubs/chat`, { accessTokenFactory: () => token })
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .build()

    conn.on('OnRoomMessage', (data: any) => {
      const from = data?.fromName || data?.from || '玩家'
      const content = data?.content || data?.text || ''
      if (!content) return
      setMessages(prev => [...prev, {
        id: Date.now(), from, text: content,
        time: new Date().toISOString(),
      }])
    })

    conn.on('OnPrivateMessage', (data: any) => {
      const from = data?.fromName || data?.from || '玩家'
      const content = data?.content || ''
      if (!content) return
      setMessages(prev => [...prev, {
        id: Date.now(), from, text: content,
        time: new Date().toISOString(),
      }])
    })

    conn.onreconnected(() => setConnected(true))
    conn.onclose(() => setConnected(false))

    conn.start().then(() => {
      setConnected(true)
      // 加入大厅频道
      conn.invoke('JoinRoom', 'lobby').catch(() => {})
    }).catch(() => {})

    connRef.current = conn
    return () => { conn.stop() }
  }, [token])

  useEffect(() => { bottomRef.current?.scrollIntoView({ behavior: 'smooth' }) }, [messages])

  const sendMessage = useCallback(async () => {
    const text = msg.trim()
    if (!text || sending || !connRef.current) return
    setSending(true)
    try {
      await connRef.current.invoke('SendRoomMessage', 'lobby', text)
      setMessages(prev => [...prev, {
        id: Date.now(), from: user?.nickname || user?.username || '我', text,
        time: new Date().toISOString(), isMine: true
      }])
      setMsg('')
    } catch { toast('发送失败', 'error') }
    setTimeout(() => setSending(false), 1500)
  }, [msg, sending, user])

  const fmt = (t: string) => new Date(t).toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit' })

  return (
    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} className="flex gap-6 h-[calc(100vh-8rem)]">
      <Card variant="default" className="flex-1 flex flex-col">
        <div className="flex items-center justify-between pb-3 border-b border-border mb-3">
          <div className="flex items-center gap-2">
            <h3 className="text-base font-bold text-white">大厅聊天</h3>
            <span className={'w-2 h-2 rounded-full ' + (connected ? 'bg-primary' : 'bg-red-500')} title={connected ? '已连接' : '未连接'} />
          </div>
          <span className="text-xs text-surface-400">{connected ? '实时' : '离线'}</span>
        </div>

        <div className="flex-1 overflow-y-auto space-y-2 mb-3 px-1">
          {messages.map(m => (
            <div key={m.id} className={'flex ' + (m.isMine ? 'justify-end' : 'justify-start')}>
              <div className={'max-w-[70%] px-3 py-2 rounded-lg text-sm ' + (m.isMine ? 'bg-primary text-surface' : 'bg-elevated text-white')}>
                {!m.isMine && <p className="text-xs text-primary font-semibold mb-0.5">{m.from}</p>}
                <p>{m.text}</p>
                <p className={'text-[10px] mt-0.5 ' + (m.isMine ? 'text-surface/60' : 'text-surface-400')}>{fmt(m.time)}</p>
              </div>
            </div>
          ))}
          <div ref={bottomRef} />
        </div>

        <div className="flex gap-2 pt-3 border-t border-border">
          <input value={msg} onChange={e => setMsg(e.target.value)}
            placeholder={connected ? '输入消息...' : '未连接...'}
            disabled={!connected}
            className="flex-1 bg-elevated border border-border rounded-md px-3 py-2.5 text-sm text-white placeholder:text-surface-500 outline-none focus:border-primary"
            onKeyDown={e => e.key === 'Enter' && sendMessage()} />
          <Button onClick={sendMessage} disabled={!connected || sending || !msg.trim()}>
            {sending ? '...' : '发送'}
          </Button>
        </div>
      </Card>
    </motion.div>
  )
}
