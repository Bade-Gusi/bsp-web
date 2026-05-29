'use client'

import { motion } from 'framer-motion'
import { Card } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'
import { Badge } from '@/components/ui/Badge'
import { api } from '@/lib/api';
import { useAuthStore } from '@/stores/authStore';

export default function ServersPage() {
  const { user } = useAuthStore();
  const servers = [
    { name: '背水竞技#1', ip: '192.168.1.100:27015', map: 'de_dust2', players: '8/10', status: 'running' },
    { name: '背水休闲#1', ip: '192.168.1.101:27015', map: 'de_mirage', players: '12/16', status: 'running' },
  ]

  return (
    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }}>
      <div className="mb-6">
        <h2 className="text-xl font-bold text-white mb-4">服务器</h2>
        <Button>创建服务器</Button>
      </div>
      <div className="space-y-3">
        {servers.map((s, i) => (
          <Card key={i} variant="hover">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-4">
                <span className={'w-3 h-3 rounded-full ' + (s.status === 'running' ? 'bg-primary' : 'bg-red-500')} />
                <div>
                  <p className="text-white font-semibold">{s.name}</p>
                  <p className="text-xs text-surface-400 font-mono">{s.ip}</p>
                </div>
              </div>
              <div className="flex items-center gap-6">
                <p className="text-sm text-white">{s.map}</p>
                <p className="text-xs text-surface-400">{s.players}</p>
                <Badge variant="success">运行中</Badge>
                <Button variant="secondary" size="sm">连接</Button>
              </div>
            </div>
          </Card>
        ))}
      </div>
    </motion.div>
  )
}
