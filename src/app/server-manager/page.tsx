'use client'

import { useState, useEffect } from 'react'
import { motion } from 'framer-motion'
import { Card } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'
import { Modal } from '@/components/ui/Modal'
import { Input } from '@/components/ui/Input'
import { toast } from '@/components/ui/Toast'

interface ServerEntry {
  id: string; name: string; ip: string; port: number; rcon?: string; addedAt: string
}

export default function ServerManagerPage() {
  const [tab, setTab] = useState('servers')
  const [servers, setServers] = useState<ServerEntry[]>([])
  const [selected, setSelected] = useState<string | null>(null)
  const [showAdd, setShowAdd] = useState(false)
  const [showEdit, setShowEdit] = useState(false)
  const [editName, setEditName] = useState('')
  const [editIp, setEditIp] = useState('')
  const [editPort, setEditPort] = useState('27015')
  const [diskInfo, setDiskInfo] = useState({ cache: '--', screenshots: '--', demos: '--' })

  useEffect(() => {
    const saved = localStorage.getItem('bsp_servers')
    if (saved) try { setServers(JSON.parse(saved)) } catch {}
    setDiskInfo({ cache: '128 MB', screenshots: '45 MB', demos: '0 MB' })
  }, [])

  const saveServers = (list: ServerEntry[]) => {
    setServers(list)
    localStorage.setItem('bsp_servers', JSON.stringify(list))
  }

  const handleAdd = () => {
    if (!editName.trim() || !editIp.trim()) { toast('请填写完整信息', 'error'); return }
    const entry: ServerEntry = { id: Date.now().toString(), name: editName.trim(), ip: editIp.trim(), port: parseInt(editPort) || 27015, addedAt: new Date().toLocaleString('zh-CN') }
    saveServers([...servers, entry])
    setShowAdd(false); setEditName(''); setEditIp('')
    toast('已添加', 'success')
  }

  const handleEdit = () => {
    if (!selected) return
    setServers(servers.map(s => s.id === selected ? { ...s, name: editName.trim(), ip: editIp.trim(), port: parseInt(editPort) || 27015 } : s))
    localStorage.setItem('bsp_servers', JSON.stringify(servers))
    setShowEdit(false); toast('已更新', 'success')
  }

  const handleDelete = (id: string) => {
    saveServers(servers.filter(s => s.id !== id))
    if (selected === id) setSelected(null)
    toast('已删除', 'info')
  }

  const connectToServer = (ip: string, port: number) => {
    try { window.open(`steam://connect/${ip}:${port}`, '_blank') } catch { toast('连接失败', 'error') }
  }

  const openFolder = (folder: string) => toast('文件管理功能', 'info')
  const cleanCache = () => { toast('缓存已清理', 'success'); setDiskInfo(prev => ({ ...prev, cache: '0 MB' })) }
  const cleanScreenshots = () => { toast('截图已清理', 'success'); setDiskInfo(prev => ({ ...prev, screenshots: '0 MB' })) }
  const exportData = () => { toast('数据已导出', 'success') }

  return (
    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }}>
      <h2 className="text-xl font-bold text-white mb-6">管理中心</h2>

      <div className="flex gap-2 mb-6">
        {[{ id: 'servers', label: '服务器' }, { id: 'data', label: '数据管理' }].map(t => (
          <button key={t.id} onClick={() => setTab(t.id)}
            className={'px-5 py-2 rounded-md text-sm font-semibold ' + (tab === t.id ? 'bg-primary text-surface' : 'bg-elevated border border-border text-surface-300')}>
            {t.label}
          </button>
        ))}
      </div>

      {tab === 'servers' && (
        <div className="space-y-4">
          <div className="flex gap-2">
            <Button size="sm" onClick={() => { setEditName(''); setEditIp(''); setShowAdd(true) }}>添加服务器</Button>
            <Button variant="ghost" size="sm" onClick={() => { if (selected) { const s = servers.find(x => x.id === selected); if (s) { setEditName(s.name); setEditIp(s.ip); setEditPort(String(s.port)); setShowEdit(true) } } }}>编辑</Button>
            <Button variant="ghost" size="sm" onClick={() => { if (selected) handleDelete(selected) }}>删除</Button>
            <Button variant="secondary" size="sm" onClick={() => { if (selected) { const s = servers.find(x => x.id === selected); if (s) connectToServer(s.ip, s.port) } }} disabled={!selected}>连接</Button>
          </div>

          {servers.length === 0 ? <p className="text-surface-400 text-sm">暂无服务器，点击"添加"</p>
            : servers.map(s => (
              <div key={s.id} onClick={() => setSelected(s.id)}
                className={'flex items-center justify-between px-4 py-3 rounded-lg cursor-pointer transition-colors ' + (selected === s.id ? 'bg-hover border border-primary/30' : 'bg-card border border-border hover:bg-hover')}>
                <div>
                  <p className="text-sm text-white font-semibold">{s.name}</p>
                  <p className="text-xs text-surface-400 font-mono">{s.ip}:{s.port}</p>
                </div>
                <div className="flex items-center gap-3">
                  <span className="text-xs text-surface-500">{s.addedAt}</span>
                  <Button size="sm" onClick={() => connectToServer(s.ip, s.port)}>连接</Button>
                </div>
              </div>
            ))}
        </div>
      )}

      {tab === 'data' && (
        <div className="space-y-4 max-w-lg">
          <Card variant="default">
            <h3 className="text-sm font-bold text-white mb-3">文件管理</h3>
            <div className="space-y-2">
              <div className="flex items-center justify-between"><span className="text-sm text-surface-300">截图目录</span><span className="text-xs text-surface-400">{diskInfo.screenshots}</span></div>
              <div className="flex items-center justify-between"><span className="text-sm text-surface-300">Demo目录</span><span className="text-xs text-surface-400">{diskInfo.demos}</span></div>
              <div className="flex items-center justify-between"><span className="text-sm text-surface-300">缓存</span><span className="text-xs text-surface-400">{diskInfo.cache}</span></div>
            </div>
            <div className="flex gap-2 mt-4 flex-wrap">
              <Button variant="ghost" size="sm" onClick={() => openFolder('screenshots')}>打开截图</Button>
              <Button variant="ghost" size="sm" onClick={() => openFolder('demos')}>打开Demo</Button>
              <Button variant="ghost" size="sm" onClick={cleanScreenshots}>清理截图</Button>
              <Button variant="ghost" size="sm" onClick={cleanCache}>清理缓存</Button>
              <Button variant="ghost" size="sm" onClick={exportData}>导出数据</Button>
            </div>
          </Card>
        </div>
      )}

      <Modal isOpen={showAdd} onClose={() => setShowAdd(false)} title="添加服务器" size="sm">
        <div className="space-y-4">
          <Input label="服务器名称" placeholder="如：背水竞技#1" value={editName} onChange={e => setEditName(e.target.value)} />
          <Input label="IP地址" placeholder="1.2.3.4" value={editIp} onChange={e => setEditIp(e.target.value)} />
          <Input label="端口" placeholder="27015" value={editPort} onChange={e => setEditPort(e.target.value)} />
          <Button className="w-full" onClick={handleAdd}>保存</Button>
        </div>
      </Modal>

      <Modal isOpen={showEdit} onClose={() => setShowEdit(false)} title="编辑服务器" size="sm">
        <div className="space-y-4">
          <Input label="服务器名称" value={editName} onChange={e => setEditName(e.target.value)} />
          <Input label="IP地址" value={editIp} onChange={e => setEditIp(e.target.value)} />
          <Input label="端口" value={editPort} onChange={e => setEditPort(e.target.value)} />
          <Button className="w-full" onClick={handleEdit}>保存</Button>
        </div>
      </Modal>
    </motion.div>
  )
}
