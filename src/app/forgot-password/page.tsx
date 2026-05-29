'use client'

import { useState } from 'react'
import { motion } from 'framer-motion'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'
import Link from 'next/link'

type Step = 1 | 2 | 3

export default function ForgotPasswordPage() {
  const [step, setStep] = useState<Step>(1)
  const [username, setUsername] = useState('')
  const [email, setEmail] = useState('')
  const [code, setCode] = useState('')
  const [newPw, setNewPw] = useState('')
  const [confirmPw, setConfirmPw] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const [countdown, setCountdown] = useState(0)

  const steps = [
    { num: 1, label: '验证身份' },
    { num: 2, label: '验证码' },
    { num: 3, label: '重置密码' },
  ]

  const sendCode = async () => {
    setError('')
    if (!username) { setError('请输入用户名'); return }
    setLoading(true)
    try {
      // API: POST /api/auth/forgot-password
      const res = await fetch('/api/auth/forgot-password', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username, email: email || undefined }),
      })
      if (!res.ok) { const e = await res.json(); throw new Error(e.error || '发送失败') }
      setStep(2)
      setCountdown(60)
      const timer = setInterval(() => {
        setCountdown(c => { if (c <= 1) { clearInterval(timer); return 0 }; return c - 1 })
      }, 1000)
    } catch (err: any) { setError(err.message) }
    finally { setLoading(false) }
  }

  const verifyCode = async () => {
    setError('')
    if (!code) { setError('请输入验证码'); return }
    if (code.length < 4) { setError('验证码格式不正确'); return }
    setLoading(true)
    try {
      const res = await fetch('/api/auth/verify-code', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username, code }),
      })
      if (!res.ok) { const e = await res.json(); throw new Error(e.error || '验证失败') }
      setStep(3)
    } catch (err: any) { setError(err.message) }
    finally { setLoading(false) }
  }

  const resetPassword = async () => {
    setError('')
    if (!newPw || newPw.length < 6) { setError('密码至少6位'); return }
    if (newPw !== confirmPw) { setError('两次密码不一致'); return }
    setLoading(true)
    try {
      const res = await fetch('/api/auth/reset-password', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username, code, newPassword: newPw }),
      })
      if (!res.ok) { const e = await res.json(); throw new Error(e.error || '重置失败') }
      // Show success, auto-redirect
      setError('')
      setStep(1)
      setUsername('')
      setEmail('')
      setCode('')
      setNewPw('')
      setConfirmPw('')
    } catch (err: any) { setError(err.message) }
    finally { setLoading(false) }
  }

  return (
    <div className="min-h-screen bg-surface flex items-center justify-center p-6">
      <motion.div initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }}
        className="w-full max-w-md">
        <div className="text-center mb-8">
          <p className="text-4xl mb-3">🔑</p>
          <h2 className="text-xl font-bold text-white">忘记密码</h2>
          <p className="text-sm text-surface-400 mt-1">重置你的账户密码</p>
        </div>

        {/* Steps indicator */}
        <div className="flex items-center justify-center gap-2 mb-8">
          {steps.map((s, i) => (
            <div key={s.num} className="flex items-center gap-2">
              <div className={'w-8 h-8 rounded-full flex items-center justify-center text-xs font-bold transition-all ' +
                (step >= s.num ? 'bg-primary text-black' : 'bg-surface-800 text-surface-400')}>
                {s.num}
              </div>
              <span className={'text-xs ' + (step >= s.num ? 'text-white' : 'text-surface-500')}>{s.label}</span>
              {i < steps.length - 1 && <div className={'w-8 h-0.5 ' + (step > s.num ? 'bg-primary' : 'bg-surface-700')} />}
            </div>
          ))}
        </div>

        <div className="bg-card border border-border rounded-xl p-6">
          {/* Step 1: Verify identity */}
          {step === 1 && (
            <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} className="space-y-4">
              <Input label="用户名" placeholder="输入你的用户名" value={username} onChange={e => setUsername(e.target.value)} />
              <Input label="邮箱（可选）" placeholder="绑定邮箱地址" value={email} onChange={e => setEmail(e.target.value)} />
              {error && <p className="text-sm text-red-400">{error}</p>}
              <Button loading={loading} className="w-full" onClick={sendCode}>发送验证码</Button>
            </motion.div>
          )}

          {/* Step 2: Verify code */}
          {step === 2 && (
            <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} className="space-y-4">
              <p className="text-sm text-surface-400">验证码已发送到你的邮箱</p>
              <Input label="验证码" placeholder="输入6位验证码" value={code} onChange={e => setCode(e.target.value.replace(/\D/g, '').slice(0, 6))} />
              {error && <p className="text-sm text-red-400">{error}</p>}
              <Button loading={loading} className="w-full" onClick={verifyCode}>验证</Button>
              <div className="text-center">
                <button onClick={sendCode} disabled={countdown > 0}
                  className={'text-xs ' + (countdown > 0 ? 'text-surface-500' : 'text-primary hover:text-accent')}>
                  {countdown > 0 ? `${countdown}s 后重新发送` : '重新发送验证码'}
                </button>
              </div>
            </motion.div>
          )}

          {/* Step 3: Reset password */}
          {step === 3 && (
            <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} className="space-y-4">
              <Input type="password" label="新密码" placeholder="至少6位" value={newPw} onChange={e => setNewPw(e.target.value)} />
              <Input type="password" label="确认新密码" placeholder="再次输入新密码" value={confirmPw} onChange={e => setConfirmPw(e.target.value)} />
              {error && <p className="text-sm text-red-400">{error}</p>}
              <Button loading={loading} className="w-full" onClick={resetPassword}>重置密码</Button>
            </motion.div>
          )}
        </div>

        <div className="text-center mt-6">
          <Link href="/" className="text-sm text-primary hover:text-accent transition-colors">
            返回登录
          </Link>
        </div>
      </motion.div>
    </div>
  )
}
