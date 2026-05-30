'use client'

import { useEffect, useState } from 'react'
import { motion } from 'framer-motion'
import { Card } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'

export default function BroadcastHistoryPage() {
  const [history, setHistory] = useState<{ addr: string; time: string }[]>([])

  useEffect(() => {
    try { const d = localStorage.getItem('broadcast_history'); if (d) setHistory(JSON.parse(d)) } catch {}
  }, [])

  return (
    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }}>
      <h2 className="text-xl font-bold text-white mb-6">广播记录</h2>
      {history.length === 0 ? (
        <Card variant="default"><p className="text-surface-400 text-sm">暂无广播记录</p></Card>
      ) : (
        <div className="space-y-2">
          {history.map((h, i) => (
            <Card key={i} variant="default" className="flex items-center justify-between">
              <div>
                <p className="text-sm text-white font-mono">{h.addr}</p>
                <p className="text-xs text-surface-400">{h.time}</p>
              </div>
              <Button size="sm" onClick={() => window.open(`steam://connect/${h.addr}`, '_blank')}>连接</Button>
            </Card>
          ))}
        </div>
      )}
    </motion.div>
  )
}
