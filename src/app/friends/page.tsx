'use client'

import { motion } from 'framer-motion'
import { Card } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'
import { api } from '@/lib/api';
import { useAuthStore } from '@/stores/authStore';

export default function FriendsPage() {
  const friends = [
    { name: 'ProPlayer', status: '在线', mmr: 1850, online: true },
    { name: 'CS2Master', status: '游戏中', mmr: 2100, online: true },
    { name: 'AceGunner', status: '离线', mmr: 1550, online: false },
    { name: 'HeadshotKing', status: '在线', mmr: 1720, online: true },
    { name: 'RushB', status: '离线', mmr: 1300, online: false },
  ]

  return (
    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }}>
      <h2 className="text-xl font-bold text-white mb-6">好友</h2>

      <div className="grid grid-cols-[1fr_2fr] gap-6">
        <div className="space-y-4">
          <Card variant="default">
            <p className="text-xs text-surface-400 mb-3">在线</p>
            {friends.filter(f => f.online).map((f, i) => (
              <div key={i} className="flex items-center gap-3 px-3 py-2 rounded-lg hover:bg-hover transition-colors cursor-pointer">
                <div className="w-9 h-9 rounded-full bg-primary/20 flex items-center justify-center text-sm text-primary font-bold">{f.name[0]}</div>
                <div className="flex-1">
                  <p className="text-sm text-white">{f.name}</p>
                  <p className="text-xs text-surface-400">{f.status} &middot; {f.mmr} MMR</p>
                </div>
                <span className={'w-2 h-2 rounded-full ' + (f.online ? 'bg-primary' : 'bg-surface-500')} />
              </div>
            ))}
          </Card>

          <Card variant="default">
            <p className="text-xs text-surface-400 mb-3">离线</p>
            {friends.filter(f => !f.online).map((f, i) => (
              <div key={i} className="flex items-center gap-3 px-3 py-2 rounded-lg hover:bg-hover transition-colors cursor-pointer">
                <div className="w-9 h-9 rounded-full bg-surface-600 flex items-center justify-center text-sm text-surface-300 font-bold">{f.name[0]}</div>
                <div className="flex-1">
                  <p className="text-sm text-surface-300">{f.name}</p>
                  <p className="text-xs text-surface-500">{f.status}</p>
                </div>
              </div>
            ))}
          </Card>
        </div>

        <Card variant="default">
          <h3 className="text-base font-bold text-white mb-4">添加好友</h3>
          <div className="flex gap-2 mb-6">
            <input placeholder="输入用户名"
              className="flex-1 bg-elevated border border-border rounded-md px-3 py-2.5 text-sm text-white placeholder:text-surface-500 outline-none focus:border-primary" />
            <Button>添加</Button>
          </div>
        </Card>
      </div>
    </motion.div>
  )
}
