'use client'

import { useState, useRef, useEffect, useCallback } from 'react'
import { motion } from 'framer-motion'
import { Card } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'
import { toast } from '@/components/ui/Toast'
import { useAuthStore } from '@/stores/authStore'
import * as signalR from '@microsoft/signalr'

const API = process.env.NEXT_PUBLIC_API_URL || ''
const CACHE_KEY = 'bsp_chat_messages'

interface ChatMsg { id: number; from: string; text: string; time: string; isMine?: boolean }

export default function ChatPage() {
  const [messages, setMessages] = useState<ChatMsg[]>(() => {
    try { const c = localStorage.getItem(CACHE_KEY); return c ? JSON.parse(c) : [] } catch { return [] }
  })
  const [msg, setMsg] = useState('')
  const [sending, setSending] = useState(false)
  const [connected, setConnected] = useState(false)
  const [loadingHistory, setLoadingHistory] = useState(true)
  const bottomRef = useRef<HTMLDivElement>(null)
  const connRef = useRef<signalR.HubConnection | null>(null)
  const { user, token } = useAuthStore()

  // localStorage缓存（保留最近1小时消息）
  useEffect(() => {
    const cutoff = Date.now() - 3600000
    const filtered = messages.filter(m => new Date(m.time).getTime() > cutoff).slice(-200)
    localStorage.setItem(CACHE_KEY, JSON.stringify(filtered))
  }, [messages])

  // 从后端拉取历史消息
  useEffect(() => {
    if (!token) return
    fetch(`${API}/api/messages`, { headers: { Authorization: `Bearer ${token}` } })
      .then(r => r.ok ? r.json() : [])
      .then((data: any[]) => {
        if (Array.isArray(data) && data.length) {
          setMessages(prev => {
            const ids = new Set(prev.map(m => m.id))
            const news = data.filter(m => !ids.has(m.id)).map(m => ({
              id: m.id, from: m.fromName || m.from || '玩家', text: m.content || m.text || '',
              time: m.createdAt || new Date().toISOString(), isMine: m.isMine || false
            }))
            return [...prev, ...news].sort((a, b) => new Date(a.time).getTime() - new Date(b.time).getTime())
          })
        }
      })
      .catch(() => {})
      .finally(() => setLoadingHistory(false))
  }, [token])

  // SignalR连接
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
      setMessages(prev => [...prev, { id: Date.now() + Math.random(), from, text: content, time: new Date().toISOString(), isMine: data?.isMine || false }])
    })
    conn.onreconnected(() => setConnected(true))
    conn.onclose(() => setConnected(false))
    conn.start().then(() => { setConnected(true); conn.invoke('JoinRoom', 'lobby').catch(() => {}) }).catch(() => {})
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
      setMessages(prev => [...prev, { id: Date.now(), from: user?.nickname || user?.username || '我', text, time: new Date().toISOString(), isMine: true }])
      setMsg('')
    } catch { toast('发送失败', 'error') }
    setTimeout(() => setSending(false), 1500)
  }, [msg, sending, user])

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
            <span className="text-xs text-surface-400">{messages.length}条消息</span>
          </div>
          <span className="text-xs text-surface-400">{connected ? '已连接' : '离线'}</span>
        </div>

        <div className="flex-1 overflow-y-auto space-y-3 my-3 px-1">
          {loadingHistory && <p className="text-center text-surface-400 text-xs py-4">加载历史消息...</p>}
          {messages.map(m => (
            <div key={m.id} className={'flex flex-col ' + (m.isMine ? 'items-end' : 'items-start')}>
              <div className={'max-w-[70%] px-3 py-2 rounded-lg text-sm ' + (m.isMine ? 'bg-primary text-surface rounded-br-md' : 'bg-elevated text-white rounded-bl-md')}>
                {!m.isMine && <p className="text-xs text-primary font-semibold mb-0.5">{m.from}</p>}
                <p className="break-words">{m.text}</p>
              </div>
              <p className={'text-[10px] mt-0.5 ' + (m.isMine ? 'text-surface-500' : 'text-surface-500')}>{fmtTime(m.time)}{m.isMine ? ' · 已发送' : ''}</p>
            </div>
          ))}
          <div ref={bottomRef} />
        </div>

        <div className="flex gap-2 pt-3 border-t border-border shrink-0">
          <input value={msg} onChange={e => setMsg(e.target.value)}
            placeholder={connected ? '输入消息...' : '未连接...'} disabled={!connected}
            className="flex-1 bg-elevated border border-border rounded-md px-3 py-2.5 text-sm text-white placeholder:text-surface-500 outline-none focus:border-primary"
            onKeyDown={e => e.key === 'Enter' && sendMessage()} />
          <Button onClick={sendMessage} disabled={!connected || sending || !msg.trim()}>{sending ? '冷却' : '发送'}</Button>
        </div>
      </Card>
    </motion.div>
  )
}
