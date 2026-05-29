'use client'

import { useState } from 'react'
import { motion } from 'framer-motion'
import { Card } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'
import { api } from '@/lib/api';
import { useAuthStore } from '@/stores/authStore';

export default function ChatPage() {
  const [msg, setMsg] = useState('')

  return (
    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }}>
      <h2 className="text-xl font-bold text-white mb-6">聊天</h2>

      <div className="grid grid-cols-[1fr_2fr] gap-6 h-[calc(100vh-8rem)]">
        <Card variant="default" className="overflow-y-auto">
          <h3 className="text-base font-bold text-white mb-4">消息</h3>
          <p className="text-sm text-surface-400">选择一个对话开始聊天</p>
        </Card>

        <Card variant="default" className="flex flex-col">
          <div className="flex-1 flex items-center justify-center">
            <p className="text-surface-400 text-sm">选择左侧对话</p>
          </div>
          <div className="flex gap-2 pt-3 border-t border-border">
            <input value={msg} onChange={(e) => setMsg(e.target.value)} placeholder="输入消息..."
              className="flex-1 bg-elevated border border-border rounded-md px-3 py-2.5 text-sm text-white placeholder:text-surface-500 outline-none focus:border-primary" />
            <Button>发送</Button>
          </div>
        </Card>
      </div>
    </motion.div>
  )
}
