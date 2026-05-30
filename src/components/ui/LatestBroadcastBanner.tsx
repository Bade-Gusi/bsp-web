'use client'

import { useEffect, useState } from 'react'
import { motion, AnimatePresence } from 'framer-motion'

export function LatestBroadcastBanner() {
  const [broadcast, setBroadcast] = useState<{ serverAddress: string; time: string } | null>(null)

  useEffect(() => {
    const fetchLatest = async () => {
      try {
        const res = await fetch('/api/admin/latest-broadcast')
        if (res.ok) {
          const data = await res.json()
          if (data?.serverAddress) {
            setBroadcast({ serverAddress: data.serverAddress, time: data.broadcastedAt || '' })
          }
        }
      } catch {}
    }
    fetchLatest()
    const interval = setInterval(fetchLatest, 30000)
    return () => clearInterval(interval)
  }, [])

  const handleConnect = () => {
    if (broadcast?.serverAddress) {
      window.open(`steam://connect/${broadcast.serverAddress}`, '_blank')
    }
  }

  return (
    <AnimatePresence>
      {broadcast && (
        <motion.div initial={{ opacity: 0, y: -20 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0, y: -20 }}
          className="mb-4">
          <div className="bg-card border border-primary/30 rounded-md px-4 py-3 flex items-center justify-between">
            <div className="flex items-center gap-3">
              <span className="w-2 h-2 rounded-full bg-primary" />
              <div>
                <span className="text-xs text-primary font-semibold">服务器广播</span>
                <p className="text-sm text-white font-mono mt-0.5">{broadcast.serverAddress}</p>
              </div>
            </div>
            <div className="flex items-center gap-3">
              <span className="text-[10px] text-surface-400">
                {broadcast.time ? new Date(broadcast.time).toLocaleString('zh-CN') : ''}
              </span>
              <button onClick={handleConnect}
                className="px-4 py-1.5 rounded-md bg-primary text-surface text-xs font-semibold hover:opacity-90 transition-opacity">
                连接
              </button>
              <button onClick={() => setBroadcast(null)}
                className="text-surface-400 hover:text-white text-sm">&times;</button>
            </div>
          </div>
        </motion.div>
      )}
    </AnimatePresence>
  )
}
