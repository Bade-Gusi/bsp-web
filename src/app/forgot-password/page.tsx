'use client'

import { useState } from 'react'
import { motion } from 'framer-motion'
import Link from 'next/link'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'

export default function ForgotPasswordPage() {
  const [step, setStep] = useState(1)
  const [email, setEmail] = useState('')
  const [code, setCode] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPw, setConfirmPw] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const [countdown, setCountdown] = useState(0)
  const [success, setSuccess] = useState(false)

  const sendCode = () => {
    if (!email) { setError('请输入邮箱'); return }
    setError('')
    setCountdown(60)
    const t = setInterval(() => {
      setCountdown(c => { if (c <= 1) { clearInterval(t); return 0 }; return c - 1 })
    }, 1000)
  }

  const handleSubmit = async () => {
    setError('')
    if (step === 1) {
      if (!email) { setError('请输入邮箱'); return }
      if (!code) { setError('请输入验证码'); return }
      setLoading(true)
      await new Promise(r => setTimeout(r, 500))
      setLoading(false)
      setStep(2)
    } else if (step === 2) {
      if (password.length < 6) { setError('密码至少6位'); return }
      if (password !== confirmPw) { setError('两次密码不一致'); return }
      setLoading(true)
      await new Promise(r => setTimeout(r, 1000))
      setLoading(false)
      setSuccess(true)
    }
  }

  if (success) return (
    <div className="min-h-screen bg-surface flex items-center justify-center">
      <motion.div initial={{ opacity: 0, scale: 0.9 }} animate={{ opacity: 1, scale: 1 }} className="card text-center p-8">
        <p className="text-primary text-xl font-bold">密码重置成功</p>
        <p className="text-surface-300 text-sm mt-2">请使用新密码登录</p>
        <Link href="/"><Button className="mt-6">返回登录</Button></Link>
      </motion.div>
    </div>
  )

  return (
    <div className="min-h-screen bg-surface flex items-center justify-center">
      <motion.div initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} className="w-[400px]">
        <div className="text-center mb-8">
          <h1 className="text-2xl font-bold text-white">忘记密码</h1>
          <p className="text-surface-400 text-sm mt-2">{step === 1 ? '验证身份' : '设置新密码'}</p>
        </div>

        <div className="card space-y-4">
          {step === 1 ? (
            <>
              <Input label="邮箱地址" placeholder="输入注册邮箱" value={email} onChange={e => setEmail(e.target.value)} />
              <div>
                <label className="text-xs font-semibold text-surface-300 mb-2 block">验证码</label>
                <div className="flex gap-2">
                  <input value={code} onChange={e => setCode(e.target.value)} placeholder="输入验证码"
                    className="flex-1 bg-elevated border border-border rounded-md px-3 py-2.5 text-sm text-white placeholder:text-surface-500 outline-none focus:border-primary" />
                  <Button variant="secondary" size="sm" onClick={sendCode} disabled={countdown > 0}>
                    {countdown > 0 ? `${countdown}s` : '发送'}
                  </Button>
                </div>
              </div>
              {error && <p className="text-red-400 text-sm">{error}</p>}
              <Button className="w-full" onClick={handleSubmit} loading={loading}>下一步</Button>
            </>
          ) : (
            <>
              <Input label="新密码" type="password" placeholder="至少6位" value={password} onChange={e => setPassword(e.target.value)} />
              <Input label="确认密码" type="password" placeholder="再次输入" value={confirmPw} onChange={e => setConfirmPw(e.target.value)} />
              {error && <p className="text-red-400 text-sm">{error}</p>}
              <Button className="w-full" onClick={handleSubmit} loading={loading}>重置密码</Button>
            </>
          )}
        </div>

        <div className="text-center mt-5">
          <Link href="/" className="text-surface-400 text-sm hover:text-primary transition-colors">返回登录</Link>
        </div>
      </motion.div>
    </div>
  )
}
