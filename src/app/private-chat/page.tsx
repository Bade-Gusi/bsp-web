'use client'

import { useState } from 'react'
import { motion } from 'framer-motion'
import { Card } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'
import { useSearchParams } from 'next/navigation'

export default function PrivateChatPage() {
  const params = useSearchParams()
  const friendId = params.get('id')
  const friendName = params.get('name') || '好友'

  const [msg, setMsg] = useState('')
  const [messages, setMessages] = useState<{ from: string; text: string; time: string }[]>([])

  const send = () => {
    if (!msg.trim()) return
    setMessages(prev => [...prev, { from: 'me', text: msg, time: new Date().toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit' }) }])
    setMsg('')
  }

  return (
    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} className="h-[calc(100vh-8rem)] flex flex-col">
      <div className="flex items-center gap-3 pb-4 border-b border-border mb-4">
        <div className="w-10 h-10 rounded-full bg-primary/20 flex items-center justify-center text-sm font-bold text-primary">{friendName[0]}</div>
        <div>
          <p className="text-white font-semibold">{friendName}</p>
          <p className="text-xs text-primary">在线</p>
        </div>
      </div>

      <div className="flex-1 overflow-y-auto space-y-3 mb-4">
        {messages.length === 0 && (
          <div className="flex items-center justify-center h-full">
            <p className="text-surface-400 text-sm">发送第一条消息</p>
          </div>
        )}
        {messages.map((m, i) => (
          <div key={i} className={'flex ' + (m.from === 'me' ? 'justify-end' : 'justify-start')}>
            <div className={'max-w-[70%] px-4 py-2.5 rounded-lg text-sm ' + (m.from === 'me' ? 'bg-primary text-surface' : 'bg-elevated text-white')}>
              <p>{m.text}</p>
              <p className={'text-[10px] mt-1 ' + (m.from === 'me' ? 'text-surface/70' : 'text-surface-400')}>{m.time}</p>
            </div>
          </div>
        ))}
      </div>

      <div className="flex gap-2 pt-3 border-t border-border">
        <input value={msg} onChange={e => setMsg(e.target.value)} placeholder="输入消息..."
          className="flex-1 bg-elevated border border-border rounded-md px-3 py-2.5 text-sm text-white placeholder:text-surface-500 outline-none focus:border-primary"
          onKeyDown={e => e.key === 'Enter' && send()} />
        <Button onClick={send}>发送</Button>
      </div>
    </motion.div>
  )
}
