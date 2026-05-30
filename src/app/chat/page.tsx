'use client'

import { useState, useRef, useEffect, useCallback } from 'react'
import { motion } from 'framer-motion'
import { Card } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'
import { useAuthStore } from '@/stores/authStore'
import * as signalR from '@microsoft/signalr'

const API = process.env.NEXT_PUBLIC_API_URL || ''
const CACHE_KEY = 'bsp_chat_cache'

interface ChatMsg {
  id: number; from: string; text: string; time: string; isMine: boolean
}

export default function ChatPage() {
  const [messages, setMessages] = useState<ChatMsg[]>(() => {
    try { const c = localStorage.getItem(CACHE_KEY); return c ? JSON.parse(c) : [] } catch { return [] }
  })
  const [msg, setMsg] = useState('')
  const [sending, setSending] = useState(false)
  const [connected, setConnected] = useState(false)
  const bottomRef = useRef<HTMLDivElement>(null)
  const connRef = useRef<signalR.HubConnection | null>(null)
  const { user, token } = useAuthStore()
  const uname = user?.nickname || user?.username || ''

  // 缓存到 localStorage（去重，保留最近200条）
  useEffect(() => {
    const deduped = messages.filter((m, i, arr) => arr.findIndex(x => x.id === m.id) === i).slice(-200)
    localStorage.setItem(CACHE_KEY, JSON.stringify(deduped))
  }, [messages])

  // 从后端拉取历史
  useEffect(() => {
    if (!token) return
    fetch(`${API}/api/messages`, { headers: { Authorization: `Bearer ${token}` } })
      .then(r => r.ok ? r.json() : [])
      .then((data: any[]) => {
        if (!Array.isArray(data) || !data.length) return
        setMessages(prev => {
          const ids = new Set(prev.map(m => m.id))
          const news = data.filter((m: any) => !ids.has(m.id)).map((m: any) => ({
            id: m.id, from: m.fromName || m.from || '玩家',
            text: m.content || m.text || '', time: m.createdAt || new Date().toISOString(),
            isMine: m.isMine || false
          }))
          return [...prev, ...news].sort((a, b) => new Date(a.time).getTime() - new Date(b.time).getTime())
        })
      })
      .catch(() => {})
  }, [token])

  // SignalR
  useEffect(() => {
    if (!token) return
    const conn = new signalR.HubConnectionBuilder()
      .withUrl(`${API}/hubs/chat`, { accessTokenFactory: () => token })
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .build()

    conn.on('OnRoomMessage', (data: any) => {
      const from = data?.fromName || data?.from || ''
      const content = data?.content || data?.text || ''
      if (!content || !from) return

      // 关键修复：如果是自己发的消息，标记 isMine=true → 显示在右侧
      // SignalR 会广播给所有人包括发送者自己，通过名字判断避免重复显示在左侧
      const isOwn = from === uname
      const id = Date.now() + Math.random()

      setMessages(prev => {
        // 去重：检查是否已存在相同内容+相同发送者的消息（1秒内）
        const dup = prev.some(m => m.from === from && m.text === content &&
          Math.abs(new Date(m.time).getTime() - Date.now()) < 2000)
        if (dup) return prev
        return [...prev, { id, from, text: content, time: new Date().toISOString(), isMine: isOwn }]
      })
    })

    conn.on('OnPrivateMessage', (data: any) => {
      const from = data?.fromName || data?.from || ''
      const content = data?.content || ''
      if (!content || !from) return
      const id = Date.now() + Math.random()
      setMessages(prev => [...prev, { id, from, text: content, time: new Date().toISOString(), isMine: from === uname }])
    })

    conn.onreconnected(() => setConnected(true))
    conn.onclose(() => setConnected(false))
    conn.start().then(() => { setConnected(true); conn.invoke('JoinRoom', 'lobby').catch(() => {}) }).catch(() => {})
    connRef.current = conn
    return () => { conn.stop() }
  }, [token, uname])

  useEffect(() => { bottomRef.current?.scrollIntoView({ behavior: 'smooth' }) }, [messages])

  const sendMessage = useCallback(async () => {
    const text = msg.trim()
    if (!text || sending || !connRef.current) return
    setSending(true)
    try {
      // 发送到 SignalR，不手动加消息（等广播回来自动显示）
      await connRef.current.invoke('SendRoomMessage', 'lobby', text)
      setMsg('')
    } catch { /* 发送失败由广播兜底 */ }
    setTimeout(() => setSending(false), 1500)
  }, [msg, sending])

  const fmtTime = (t: string) => {
    const d = new Date(t); const now = new Date()
    if (d.toDateString() === now.toDateString()) return d.toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit' })
    return `${d.getMonth() + 1}/${d.getDate()} ${d.toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit' })}`
  }

  return (
    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} className="h-[calc(100vh-8rem)]">
      <Card variant="default" className="h-full flex flex-col">
        <div className="flex items-center justify-between pb-3 border-b border-border shrink-0">
          <div className="flex items-center gap-2">
            <span className={'w-2 h-2 rounded-full ' + (connected ? 'bg-primary' : 'bg-red-500')} />
            <h3 className="text-base font-bold text-white">大厅聊天</h3>
            <span className="text-xs text-surface-400">{messages.length}条</span>
          </div>
          <span className="text-xs text-surface-400">{connected ? '在线' : '离线'}</span>
        </div>

        <div className="flex-1 overflow-y-auto space-y-3 my-3 px-1">
          {messages.map(m => (
            <div key={m.id} className={'flex flex-col ' + (m.isMine ? 'items-end' : 'items-start')}>
              <div className={'max-w-[70%] px-3 py-2 rounded-lg text-sm ' + (
                m.isMine ? 'bg-primary text-surface rounded-br-md' : 'bg-elevated text-white rounded-bl-md'
              )}>
                {!m.isMine && <p className="text-xs text-primary font-semibold mb-0.5">{m.from}</p>}
                <p className="break-words">{m.text}</p>
              </div>
              <p className="text-[10px] text-surface-500 mt-0.5">{fmtTime(m.time)}</p>
            </div>
          ))}
          <div ref={bottomRef} />
        </div>

        <div className="flex gap-2 pt-3 border-t border-border shrink-0">
          <input value={msg} onChange={e => setMsg(e.target.value)}
            placeholder={connected ? '输入消息...' : '未连接...'} disabled={!connected}
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
