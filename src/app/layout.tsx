'use client'

import { useEffect, useState, useRef } from 'react'
import { usePathname } from 'next/navigation'
import { AnimatePresence, motion } from 'framer-motion'
import '@/styles/globals.css'
import { Sidebar } from '@/components/layout/Sidebar'
import { Header } from '@/components/layout/Header'
import { LoadingOverlay } from '@/components/layout/LoadingOverlay'
import { useAuthStore } from '@/stores/authStore'
import { api } from '@/lib/api'
import { AuthPage } from '@/components/auth/AuthPage'

const publicPaths = ['/']

// 页面过渡动画
const pageTransition = {
  initial: { opacity: 0, y: 20, scale: 0.98 },
  animate: { opacity: 1, y: 0, scale: 1 },
  exit: { opacity: 0, y: -10, scale: 0.98 },
  transition: { duration: 0.35, ease: [0.16, 1, 0.3, 1] }
}

export default function RootLayout({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, loading, token, loadFromStorage } = useAuthStore()
  const pathname = usePathname()
  const [pageTitle, setPageTitle] = useState('首页')
  const [init, setInit] = useState(false)
  const [particles] = useState(() =>
    Array.from({ length: 20 }, (_, i) => ({
      id: i, x: Math.random() * 100, y: Math.random() * 100,
      size: Math.random() * 3 + 1, delay: Math.random() * 5, duration: Math.random() * 5 + 5
    }))
  )

  // 初始化：从 localStorage 恢复登录态
  useEffect(() => {
    loadFromStorage()
    // 如果已有 token，设置到 API 客户端
    const t = localStorage.getItem('bsp_token')
    if (t) api.setToken(t)
    setTimeout(() => setInit(true), 300)
  }, [])

  // 监听 token 变化同步到 API 客户端
  useEffect(() => { api.setToken(token) }, [token])

  useEffect(() => {
    const titles: Record<string, string> = {
      '/dashboard': '首页', '/match': '快速匹配', '/duel': '1v1对战', '/rooms': '房间大厅',
      '/servers': '服务器', '/friends': '好友', '/chat': '聊天', '/leaderboard': '排行榜',
      '/achievements': '成就', '/market': '皮肤市场', '/welfare': '背水公益',
      '/settings': '设置', '/minigames': '小游戏',
    }
    setPageTitle(titles[pathname] || '背水对战平台')
  }, [pathname])

  if (loading || !init) return <LoadingOverlay />

  if (!isAuthenticated && !publicPaths.includes(pathname)) return <AuthPage />
  if (pathname === '/') return <AuthPage />

  return (
    <html lang="zh-CN" className="dark">
      <head>
        <meta charSet="utf-8" />
        <meta name="viewport" content="width=device-width, initial-scale=1" />
        <title>背水对战平台</title>
      </head>
      <body className="bg-surface text-white overflow-hidden">
        {/* 背景粒子 */}
        <div className="fixed inset-0 pointer-events-none z-0 overflow-hidden">
          {particles.map((p) => (
            <motion.div key={p.id}
              className="absolute rounded-full bg-primary/10"
              style={{ left: `${p.x}%`, top: `${p.y}%`, width: p.size, height: p.size }}
              animate={{ y: [0, -30, 0], opacity: [0.1, 0.3, 0.1] }}
              transition={{ repeat: Infinity, duration: p.duration, delay: p.delay, ease: 'easeInOut' }}
            />
          ))}
        </div>

        <div className="flex h-screen relative z-10">
          <Sidebar />
          <div className="flex-1 flex flex-col overflow-hidden">
            <Header pageTitle={pageTitle} />
            <main className="flex-1 overflow-y-auto p-6">
              <AnimatePresence mode="wait">
                <motion.div
                  key={pathname}
                  initial={pageTransition.initial}
                  animate={pageTransition.animate}
                  exit={pageTransition.exit}
                  transition={pageTransition.transition}
                >
                  {children}
                </motion.div>
              </AnimatePresence>
            </main>
          </div>
        </div>
      </body>
    </html>
  )
}
