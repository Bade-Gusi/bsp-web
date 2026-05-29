'use client'

import { useState } from 'react'
import { motion, AnimatePresence } from 'framer-motion'
import { LoginForm } from './LoginForm'
import { RegisterForm } from './RegisterForm'

export function AuthPage() {
  const [mode, setMode] = useState<'login' | 'register'>('login')

  return (
    <html lang="zh-CN" className="dark">
      <head>
        <meta charSet="utf-8" />
        <meta name="viewport" content="width=device-width, initial-scale=1" />
        <title>背水对战平台 - 登录</title>
      </head>
      <body className="bg-surface text-white">
        <div className="min-h-screen flex items-center justify-center relative overflow-hidden">
          {/* 背景光晕 */}
          <div className="absolute -top-40 -left-40 w-80 h-80 bg-primary/5 rounded-full blur-[120px]" />
          <div className="absolute -bottom-20 -right-20 w-60 h-60 bg-accent/5 rounded-full blur-[100px]" />

          <motion.div
            initial={{ opacity: 0, scale: 0.96 }}
            animate={{ opacity: 1, scale: 1 }}
            transition={{ duration: 0.5, ease: [0.16, 1, 0.3, 1] }}
            className="w-[420px]"
          >
            {/* Logo */}
            <div className="text-center mb-9">
              <div className="w-20 h-20 mx-auto mb-5 rounded-[22px] bg-elevated border border-primary/30 flex items-center justify-center">
                <span className="text-3xl">🎮</span>
              </div>
              <h1 className="text-3xl font-bold">背水对战平台</h1>
              <p className="text-surface-400 text-xs font-mono mt-2">BEISHUI AURORA v2.0</p>
            </div>

            {/* 卡片 */}
            <div className="card">
              <AnimatePresence mode="wait">
                {mode === 'login' ? (
                  <LoginForm key="login" onSwitchToRegister={() => setMode('register')} />
                ) : (
                  <RegisterForm key="register" onSwitchToLogin={() => setMode('login')} />
                )}
              </AnimatePresence>
            </div>

            {/* 底部链接 */}
            <div className="flex justify-center gap-6 mt-6">
              <button onClick={() => setMode(mode === 'login' ? 'register' : 'login')}
                className="text-surface-400 text-sm hover:text-primary transition-colors">
                {mode === 'login' ? '注册账号' : '返回登录'}
              </button>
              <button className="text-surface-400 text-sm hover:text-primary transition-colors">
                忘记密码
              </button>
              <button className="text-surface-400 text-sm hover:text-primary transition-colors">
                服务器
              </button>
            </div>
            <div className="flex justify-center gap-4 mt-4">
              <button className="text-surface-400 text-xs underline hover:text-primary transition-colors">用户协议</button>
              <span className="text-surface-600 text-xs">|</span>
              <button className="text-surface-400 text-xs underline hover:text-primary transition-colors">隐私政策</button>
              <span className="text-surface-600 text-xs">|</span>
              <button className="text-surface-400 text-xs underline hover:text-primary transition-colors">平台声明</button>
            </div>
            <p className="text-center text-surface-500 text-xs font-mono mt-6">v2.0.0 Aurora</p>
          </motion.div>
        </div>
      </body>
    </html>
  )
}
