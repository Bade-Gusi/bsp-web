'use client'

import { motion } from 'framer-motion'
import { useState, useEffect } from 'react'
import { Card } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'
import { Badge } from '@/components/ui/Badge'
import { Modal } from '@/components/ui/Modal'
import { Input } from '@/components/ui/Input'
import { api } from '@/lib/api'
import { useAuthStore } from '@/stores/authStore'

interface Server {
  code: string
  name: string
  ip?: string
  port?: number
  map?: string
  currentPlayers?: number
  maxPlayers?: number
  status?: string
  mode?: string
  region?: string
  createdAt?: string
}

export default function ServersPage() {
  const [servers, setServers] = useState<Server[]>([])
  const [loading, setLoading] = useState(true)
  const [showCreate, setShowCreate] = useState(false)
  const [creating, setCreating] = useState(false)
  const [error, setError] = useState('')
  const { user } = useAuthStore()

  // Create form
  const [serverName, setServerName] = useState('')
  const [serverMap, setServerMap] = useState('de_dust2')
  const [serverMode, setServerMode] = useState('competitive')
  const [serverPassword, setServerPassword] = useState('')

  const MAPS = ['de_dust2', 'de_mirage', 'de_inferno', 'de_nuke', 'de_overpass', 'de_ancient', 'de_vertigo', 'de_anubis']

  const fetchServers = async () => {
    try {
      setLoading(true)
      const data = await api.getServers()
      setServers(Array.isArray(data) ? data : [])
    } catch {}
    finally { setLoading(false) }
  }

  useEffect(() => { fetchServers() }, [])

  const handleCreate = async () => {
    setError('')
    if (!serverName.trim()) { setError('请输入服务器名称'); return }
    setCreating(true)
    try {
      await api.createServer({
        name: serverName.trim(),
        map: serverMap,
        mode: serverMode,
        password: serverPassword || undefined,
      })
      setShowCreate(false)
      setServerName('')
      setServerPassword('')
      fetchServers()
    } catch (err: any) { setError(err.message || '创建失败') }
    finally { setCreating(false) }
  }

  const handleDelete = async (code: string) => {
    try { await api.deleteServer(code); fetchServers() }
    catch { /* ignore */ }
  }

  return (
    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }}>
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-xl font-bold text-white">服务器</h2>
        <Button onClick={() => setShowCreate(true)}>创建服务器</Button>
      </div>

      {loading ? (
        <div className="flex justify-center py-20">
          <div className="w-8 h-8 rounded-full border-[3px] border-primary border-t-transparent animate-spin" />
        </div>
      ) : servers.length === 0 ? (
        <Card variant="default" className="text-center py-20">
          <p className="text-4xl mb-4">🖥️</p>
          <p className="text-surface-400">暂无服务器，点击右上角创建</p>
        </Card>
      ) : (
        <div className="space-y-3">
          {servers.map((s, i) => (
            <Card key={s.code || i} variant="hover">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-4">
                  <span className={'w-3 h-3 rounded-full ' + (s.status !== 'stopped' ? 'bg-primary' : 'bg-red-500')} />
                  <div>
                    <p className="text-white font-semibold">{s.name}</p>
                    <p className="text-xs text-surface-400 font-mono">
                      {s.ip || 'N/A'}:{s.port || '--'}
                    </p>
                  </div>
                </div>
                <div className="flex items-center gap-6">
                  <p className="text-sm text-white">{s.map || 'N/A'}</p>
                  <p className="text-xs text-surface-400">
                    {s.currentPlayers || 0}/{s.maxPlayers || '--'}
                  </p>
                  <Badge variant={s.status !== 'stopped' ? 'success' : 'danger'}>
                    {s.status !== 'stopped' ? '运行中' : '已停止'}
                  </Badge>
                  <div className="flex gap-2">
                    <Button variant="secondary" size="sm">连接</Button>
                    <Button variant="ghost" size="sm" onClick={() => handleDelete(s.code)}>删除</Button>
                  </div>
                </div>
              </div>
            </Card>
          ))}
        </div>
      )}

      <Modal isOpen={showCreate} onClose={() => { setShowCreate(false); setError('') }} title="创建服务器" size="md">
        <div className="space-y-4">
          <Input label="服务器名称" placeholder="输入名称" value={serverName} onChange={e => setServerName(e.target.value)} />
          <div>
            <p className="text-sm font-medium text-surface-300 mb-2">地图</p>
            <div className="grid grid-cols-4 gap-2">
              {MAPS.map(m => (
                <button key={m} onClick={() => setServerMap(m)}
                  className={'px-3 py-2 rounded-lg text-xs font-medium transition-all ' +
                    (serverMap === m ? 'bg-primary text-black' : 'bg-surface-800 text-surface-300 hover:bg-surface-700')}>
                  {m}
                </button>
              ))}
            </div>
          </div>
          <Input label="密码（可选）" type="password" placeholder="留空为公开" value={serverPassword} onChange={e => setServerPassword(e.target.value)} />
          {error && <p className="text-sm text-red-400">{error}</p>}
          <Button loading={creating} className="w-full" onClick={handleCreate}>创建</Button>
        </div>
      </Modal>
    </motion.div>
  )
}
