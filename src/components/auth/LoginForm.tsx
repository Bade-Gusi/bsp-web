'use client'

import { useState } from 'react'
import { motion } from 'framer-motion'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'
import { useAuth } from '@/hooks/useAuth'

export function LoginForm({ onSwitchToRegister }: { onSwitchToRegister: () => void }) {
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const { login } = useAuth()

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    if (!username || !password) { setError('请填写用户名和密码'); return }
    setLoading(true)
    try { await login(username, password) }
    catch (err: any) { setError(err.message || '登录失败') }
    finally { setLoading(false) }
  }

  return (
    <motion.form key="login" initial={{ opacity: 0, x: -20 }} animate={{ opacity: 1, x: 0 }} exit={{ opacity: 0, x: 20 }} onSubmit={handleSubmit} className="space-y-5">
      <Input label="用户名" placeholder="输入用户名" value={username} onChange={(e) => setUsername(e.target.value)} icon="👤" />
      <Input label="密码" type="password" placeholder="输入密码" value={password} onChange={(e) => setPassword(e.target.value)} icon="🔒" />
      {error && <p className="text-red-400 text-sm">{error}</p>}
      <Button loading={loading} className="w-full h-12 text-base">登 录</Button>
      <Button variant="secondary" onClick={() => {}} className="w-full h-12">
        <span>&#xE777;</span> 使用 Steam 登录
      </Button>
    </motion.form>
  )
}
