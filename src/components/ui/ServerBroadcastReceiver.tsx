'use client'

import { useState, useEffect } from 'react'
import { motion, AnimatePresence } from 'framer-motion'
import { Button } from './Button'
import * as signalR from '@microsoft/signalr'
import { useAuthStore } from '@/stores/authStore'

const API = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000'

interface BroadcastData {
  serverAddress: string
  adminName: string
  timestamp: string
}

export function ServerBroadcastReceiver() {
  const [broadcast, setBroadcast] = useState<BroadcastData | null>(null)
  const [dismissed, setDismissed] = useState(false)
  const token = useAuthStore(s => s.token)

  useEffect(() => {
    if (!token) return
    let conn: signalR.HubConnection | null = null

    const connect = async () => {
      conn = new signalR.HubConnectionBuilder()
        .withUrl(`${API}/hubs/broadcast`, { accessTokenFactory: () => token })
        .withAutomaticReconnect([0, 2000, 5000])
        .build()

      conn.on('OnServerBroadcast', (data: BroadcastData) => {
        setBroadcast(data)
        setDismissed(false)
      })

      try { await conn.start() } catch {}
    }

    connect()
    return () => { conn?.stop() }
  }, [token])

  const handleConnect = () => {
    if (broadcast?.serverAddress) {
      window.open(`steam://connect/${broadcast.serverAddress}`, '_blank')
    }
  }

  return (
    <AnimatePresence>
      {broadcast && !dismissed && (
        <motion.div
          initial={{ opacity: 0, y: 50, x: '-50%' }}
          animate={{ opacity: 1, y: 0, x: '-50%' }}
          exit={{ opacity: 0, y: 50, x: '-50%' }}
          className="fixed bottom-6 left-1/2 z-[9999] w-96"
        >
          <div className="bg-card border border-primary/40 rounded-xl shadow-surface p-5">
            <div className="flex items-center justify-between mb-3">
              <div className="flex items-center gap-2">
                <span className="w-2 h-2 rounded-full bg-primary animate-pulse" />
                <span className="text-xs text-primary font-semibold">服务器广播</span>
              </div>
              <button onClick={() => setDismissed(true)} className="text-surface-400 hover:text-white text-sm">&times;</button>
            </div>
            <p className="text-white font-semibold mb-1">管理员发布了新的服务器地址</p>
            <p className="text-primary font-mono text-sm mb-1">{broadcast.serverAddress}</p>
            <p className="text-xs text-surface-400 mb-4">
              来自: {broadcast.adminName} · {broadcast.timestamp ? new Date(broadcast.timestamp).toLocaleTimeString('zh-CN') : ''}
            </p>
            <div className="flex gap-2">
              <Button size="sm" onClick={handleConnect}>连接服务器</Button>
              <Button variant="ghost" size="sm" onClick={() => {
                navigator.clipboard.writeText(broadcast.serverAddress)
              }}>复制地址</Button>
            </div>
          </div>
        </motion.div>
      )}
    </AnimatePresence>
  )
}
