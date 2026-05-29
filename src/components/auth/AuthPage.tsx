'use client'

import { useState } from 'react'
import { motion, AnimatePresence } from 'framer-motion'
import { LoginForm } from './LoginForm'
import { RegisterForm } from './RegisterForm'

export function AuthPage() {
  const [mode, setMode] = useState<'login' | 'register'>('login')

  return (
    <div className="min-h-screen flex items-center justify-center bg-surface">
      <motion.div
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.5 }}
        className="w-[400px]"
      >
        <div className="text-center mb-8">
          <img src="/bsp-web/default_avatar.png" className="w-16 h-16 mx-auto mb-4 rounded-xl bg-elevated border border-primary/30 object-cover" alt="logo" />
          <h1 className="text-2xl font-bold text-white">背水对战平台</h1>
          <p className="text-surface-400 text-xs font-mono mt-2">BEISHUI</p>
        </div>

        <div className="card">
          <AnimatePresence mode="wait">
            {mode === 'login' ? (
              <LoginForm key="login" onSwitchToRegister={() => setMode('register')} />
            ) : (
              <RegisterForm key="register" onSwitchToLogin={() => setMode('login')} />
            )}
          </AnimatePresence>
        </div>

        <div className="flex justify-center gap-6 mt-5">
          <button onClick={() => setMode(mode === 'login' ? 'register' : 'login')}
            className="text-surface-400 text-sm hover:text-primary transition-colors">
            {mode === 'login' ? '注册账号' : '返回登录'}
          </button>
          <button className="text-surface-400 text-sm hover:text-primary transition-colors">忘记密码</button>
        </div>
        <div className="flex justify-center gap-4 mt-3">
          <button className="text-surface-500 text-xs hover:text-primary transition-colors">用户协议</button>
          <span className="text-surface-600 text-xs">|</span>
          <button className="text-surface-500 text-xs hover:text-primary transition-colors">隐私政策</button>
          <span className="text-surface-600 text-xs">|</span>
          <button className="text-surface-500 text-xs hover:text-primary transition-colors">平台声明</button>
        </div>
      </motion.div>
    </div>
  )
}
