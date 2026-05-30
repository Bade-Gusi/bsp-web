'use client'

import { useState, useEffect } from 'react'
import { motion } from 'framer-motion'
import { Card } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'
import { toast } from '@/components/ui/Toast'
import { api } from '@/lib/api'

const ADMIN_PASSWORD = 'beishui888'
const COOLDOWN_MS = 3000

export default function BroadcastPage() {
  const [password, setPassword] = useState('')
  const [verified, setVerified] = useState(false)
  const [serverAddress, setServerAddress] = useState('1.2.3.4:27015')
  const [status, setStatus] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [lastSend, setLastSend] = useState(0)
  const [history, setHistory] = useState<{ addr: string; time: string }[]>([])
  const [refreshing, setRefreshing] = useState(false)

  // 从 localStorage 加载广播历史
  useEffect(() => {
    try { const d = localStorage.getItem('broadcast_history'); if (d) setHistory(JSON.parse(d)) } catch {}
  }, [])

  const checkCooldown = () => {
    const remaining = COOLDOWN_MS - (Date.now() - lastSend)
    if (remaining > 0) { setError('请等待 ' + Math.ceil(remaining / 1000) + ' 秒后重试'); return false }
    return true
  }

  const handlePassword = () => {
    setError('')
    if (password === ADMIN_PASSWORD) { setVerified(true) }
    else { setError('密码错误') }
  }

  const handleBroadcast = async () => {
    setError(''); setStatus('')
    if (!checkCooldown()) return
    if (!serverAddress.trim()) { setError('请输入服务器地址'); return }
    setLoading(true)
    try {
      const res = await fetch('/api/admin/broadcast', {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ serverAddress: serverAddress.trim(), password }),
      })
      if (!res.ok) { const e = await res.json(); throw new Error(e.error || '广播失败') }
      toast('广播成功，所有在线用户已收到', 'success')
      setStatus('✅ 广播成功')
      setLastSend(Date.now())
      const entry = { addr: serverAddress.trim(), time: new Date().toLocaleString('zh-CN') }
      const upd = [entry, ...history].slice(0, 10)
      setHistory(upd)
      localStorage.setItem('broadcast_history', JSON.stringify(upd))
    } catch (err: any) { setError(err.message || '广播失败') }
    setLoading(false)
  }

  // 刷新：从后端获取最新广播的服务器地址（替代 SignalR 监听）
  const handleRefresh = async () => {
    setRefreshing(true)
    setError('')
    try {
      const res = await fetch('/api/admin/latest-broadcast')
      if (res.ok) {
        const data = await res.json()
        if (data?.serverAddress) {
          toast('获取到服务器地址: ' + data.serverAddress, 'info')
        } else {
          toast('暂无广播记录', 'info')
        }
      }
    } catch { setError('获取失败') }
    setRefreshing(false)
  }

  return (
    <div className="min-h-screen bg-surface flex flex-col items-center justify-center p-6">
      <motion.div initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} className="w-[420px]">
        <Card variant="default" className="p-8">
          <div className="text-center mb-6">
            <h2 className="text-xl font-bold text-white">广播服务器地址</h2>
            <p className="text-sm text-surface-400 mt-1">输入管理密码后将地址推送到所有在线用户</p>
          </div>

          {!verified ? (
            <div className="space-y-4">
              <Input label="管理密码" type="password" placeholder="输入管理密码" value={password} onChange={e => setPassword(e.target.value)} />
              {error && <p className="text-red-400 text-sm">{error}</p>}
              <Button className="w-full" onClick={handlePassword}>验证</Button>
            </div>
          ) : (
            <div className="space-y-4">
              <Input label="服务器地址" placeholder="IP:端口" value={serverAddress} onChange={e => setServerAddress(e.target.value)} />
              {error && <p className="text-red-400 text-sm">{error}</p>}
              {status && <p className="text-primary text-sm">{status}</p>}
              <Button className="w-full" onClick={handleBroadcast} loading={loading} disabled={loading}>广播给所有在线用户</Button>

              <div className="pt-4 border-t border-border">
                <div className="flex items-center justify-between mb-3">
                  <p className="text-sm text-surface-400">获取最新广播</p>
                  <Button variant="ghost" size="sm" onClick={handleRefresh} disabled={refreshing}>
                    {refreshing ? '刷新中...' : '刷新'}
                  </Button>
                </div>
                {history.length > 0 && (
                  <div className="space-y-1">
                    {history.map((h, i) => (
                      <div key={i} className="flex items-center justify-between px-3 py-1.5 rounded bg-elevated/50 text-xs">
                        <span className="text-primary font-mono">{h.addr}</span>
                        <span className="text-surface-500">{h.time}</span>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </div>
          )}
        </Card>
      </motion.div>
    </div>
  )
}
