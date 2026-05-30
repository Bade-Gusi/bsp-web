'use client'

import { useState } from 'react'
import { motion } from 'framer-motion'
import { Card } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'

const ADMIN_PASSWORD = 'beishui888'

export default function BroadcastPage() {
  const [password, setPassword] = useState('')
  const [verified, setVerified] = useState(false)
  const [serverAddress, setServerAddress] = useState('1.2.3.4:27015')
  const [status, setStatus] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  const handlePassword = () => {
    setError('')
    if (password === ADMIN_PASSWORD) {
      setVerified(true)
    } else {
      setError('密码错误')
    }
  }

  const handleBroadcast = async () => {
    if (!serverAddress.trim()) { setError('请输入服务器地址'); return }
    setLoading(true)
    setStatus('')
    setError('')

    try {
      const res = await fetch('/api/admin/broadcast', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ serverAddress: serverAddress.trim() }),
      })
      if (!res.ok) { const e = await res.json(); throw new Error(e.error || '广播失败') }
      setStatus('广播成功，所有在线用户已收到')
    } catch (err: any) {
      setError(err.message || '广播失败')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen bg-surface flex items-center justify-center p-6">
      <motion.div initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} className="w-[420px]">
        <Card variant="default" className="p-8">
          <div className="text-center mb-6">
            <h2 className="text-xl font-bold text-white">广播服务器地址</h2>
            <p className="text-sm text-surface-400 mt-1">输入管理密码后将地址推送到所有在线用户</p>
          </div>

          {!verified ? (
            <div className="space-y-4">
              <Input label="管理密码" type="password" placeholder="输入管理密码" value={password}
                onChange={e => setPassword(e.target.value)} />
              {error && <p className="text-red-400 text-sm">{error}</p>}
              <Button className="w-full" onClick={handlePassword}>验证</Button>
            </div>
          ) : (
            <div className="space-y-4">
              <Input label="服务器地址" placeholder="IP:端口" value={serverAddress}
                onChange={e => setServerAddress(e.target.value)} />
              {error && <p className="text-red-400 text-sm">{error}</p>}
              {status && <p className="text-primary text-sm">{status}</p>}
              <Button className="w-full" onClick={handleBroadcast} loading={loading}>
                广播给所有在线用户
              </Button>
            </div>
          )}
        </Card>
      </motion.div>
    </div>
  )
}
