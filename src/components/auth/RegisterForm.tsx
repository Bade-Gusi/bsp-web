'use client'

import { useState } from 'react'
import { motion } from 'framer-motion'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'
import { useAuth } from '@/hooks/useAuth'

export function RegisterForm({ onSwitchToLogin }: { onSwitchToLogin: () => void }) {
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPw, setConfirmPw] = useState('')
  const [nickname, setNickname] = useState('')
  const [error, setError] = useState('')
  const [success, setSuccess] = useState(false)
  const [loading, setLoading] = useState(false)
  const { register } = useAuth()

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    if (!username || !password || !nickname) { setError('请填写所有必填项'); return }
    if (password.length < 6) { setError('密码至少6位'); return }
    if (password !== confirmPw) { setError('两次密码不一致'); return }
    setLoading(true)
    try {
      await register(username, password, nickname)
      setSuccess(true)
      setTimeout(onSwitchToLogin, 1500)
    } catch (err: any) { setError(err.message || '注册失败') }
    finally { setLoading(false) }
  }

  if (success) return (
    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} className="text-center py-8">
      <p className="text-primary text-lg font-bold">注册成功！</p>
      <p className="text-surface-300 mt-2">即将跳转到登录页...</p>
    </motion.div>
  )

  return (
    <motion.form key="register" initial={{ opacity: 0, x: 20 }} animate={{ opacity: 1, x: 0 }} exit={{ opacity: 0, x: -20 }} onSubmit={handleSubmit} className="space-y-4">
      <Input label="用户名" placeholder="登录用" value={username} onChange={(e) => setUsername(e.target.value)} />
      <Input label="昵称" placeholder="游戏中显示的名称" value={nickname} onChange={(e) => setNickname(e.target.value)} />
      <Input label="密码" type="password" placeholder="至少6位" value={password} onChange={(e) => setPassword(e.target.value)} />
      <Input label="确认密码" type="password" placeholder="再次输入密码" value={confirmPw} onChange={(e) => setConfirmPw(e.target.value)} />
      {error && <p className="text-red-400 text-sm">{error}</p>}
      <Button loading={loading} className="w-full h-12 text-base">创建账号</Button>
    </motion.form>
  )
}
